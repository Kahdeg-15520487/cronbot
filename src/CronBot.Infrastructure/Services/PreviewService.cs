using CronBot.Infrastructure.Data;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Service for managing preview containers and taking screenshots.
/// </summary>
public class PreviewService
{
    private readonly DockerClientService _docker;
    private readonly AppDbContext _context;
    private readonly ILogger<PreviewService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _networkName;

    public PreviewService(
        DockerClientService docker,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<PreviewService> logger)
    {
        _docker = docker;
        _context = context;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _networkName = configuration["Docker:Network"] ?? "cronbot_default";
    }

    /// <summary>
    /// Start a preview container for a project.
    /// </summary>
    public async Task<PreviewResult?> StartPreviewAsync(
        Guid projectId,
        int port = 3000,
        string? command = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the project's agent to find workspace
            var agent = await _context.Agents
                .Where(a => a.ProjectId == projectId && a.Status != Domain.Enums.AgentStatus.Terminated)
                .FirstOrDefaultAsync(cancellationToken);

            if (agent == null)
            {
                _logger.LogWarning("No active agent found for project {ProjectId}", projectId);
                return null;
            }

            var previewId = Guid.NewGuid();
            var containerName = $"cronbot-preview-{previewId.ToString()[..8]}";

            // Stop any existing preview for this project
            await StopProjectPreviewAsync(projectId, cancellationToken);

            // Create preview container
            var environment = new List<string>
            {
                $"PORT={port}",
                "NODE_ENV=production"
            };

            var labels = new Dictionary<string, string>
            {
                ["cronbot.managed"] = "true",
                ["cronbot.preview"] = "true",
                ["cronbot.project-id"] = projectId.ToString()
            };

            var createParams = new CreateContainerParameters
            {
                Name = containerName,
                Image = "node:20-slim",
                Env = environment,
                Labels = labels,
                // Default to running npm start
                Cmd = command != null
                    ? new List<string> { "sh", "-c", command }
                    : new List<string> { "sh", "-c", "npm install && npm run build && npm start" },
                WorkingDir = "/workspace",
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        [$"{port}/tcp"] = new List<PortBinding>
                        {
                            new PortBinding { HostPort = "0" } // Auto-assign port
                        }
                    },
                    Binds = new List<string>
                    {
                        $"agent-{agent.Id}-workspace:/workspace:ro"
                    },
                    NetworkMode = _networkName
                },
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    [$"{port}/tcp"] = new EmptyStruct()
                }
            };

            var container = await _docker.GetClient().Containers.CreateContainerAsync(
                createParams,
                cancellationToken);

            if (container?.ID == null)
            {
                _logger.LogError("Failed to create preview container for project {ProjectId}", projectId);
                return null;
            }

            // Start the container
            await _docker.GetClient().Containers.StartContainerAsync(
                container.ID,
                new ContainerStartParameters(),
                cancellationToken);

            // Get the assigned port
            var inspect = await _docker.GetClient().Containers.InspectContainerAsync(
                container.ID,
                cancellationToken);

            string? hostPort = null;
            if (inspect.NetworkSettings?.Ports != null &&
                inspect.NetworkSettings.Ports.TryGetValue($"{port}/tcp", out var portBindings))
            {
                hostPort = portBindings?.FirstOrDefault()?.HostPort;
            }
            var actualPort = int.TryParse(hostPort, out var p) ? p : port;

            _logger.LogInformation(
                "Started preview container {ContainerName} for project {ProjectId} on port {Port}",
                containerName, projectId, actualPort);

            return new PreviewResult
            {
                PreviewId = previewId,
                ContainerId = container.ID,
                ContainerName = containerName,
                ProjectId = projectId,
                Port = actualPort,
                Url = $"http://localhost:{actualPort}",
                StartedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start preview for project {ProjectId}", projectId);
            return null;
        }
    }

    /// <summary>
    /// Take a screenshot of a preview using Puppeteer service.
    /// </summary>
    public async Task<ScreenshotResult?> TakeScreenshotAsync(
        string previewUrl,
        string? selector = null,
        int? width = 1280,
        int? height = 720,
        int waitForMs = 2000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For MVP, we'll use a simple HTTP request to check if the page is up
            // A full implementation would use a Puppeteer container

            _logger.LogInformation("Taking screenshot of {Url}", previewUrl);

            // Wait for the page to be ready
            await Task.Delay(waitForMs, cancellationToken);

            var response = await _httpClient.GetAsync(previewUrl, cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;

            if (!isHealthy)
            {
                _logger.LogWarning("Preview at {Url} returned {StatusCode}", previewUrl, response.StatusCode);
            }

            // For MVP, return a placeholder result
            // TODO: Implement actual Puppeteer screenshot
            return new ScreenshotResult
            {
                Success = isHealthy,
                Url = previewUrl,
                Message = isHealthy
                    ? $"Preview is accessible at {previewUrl}"
                    : $"Preview returned status {response.StatusCode}",
                ScreenshotUrl = null, // Would be a URL to the screenshot image
                CapturedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot of {Url}", previewUrl);
            return new ScreenshotResult
            {
                Success = false,
                Url = previewUrl,
                Message = $"Error: {ex.Message}",
                CapturedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Stop a preview container.
    /// </summary>
    public async Task<bool> StopPreviewAsync(
        string containerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _docker.StopContainerAsync(containerId, cancellationToken);
            await _docker.RemoveContainerAsync(containerId, force: true, cancellationToken);
            _logger.LogInformation("Stopped preview container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop preview container {ContainerId}", containerId);
            return false;
        }
    }

    /// <summary>
    /// Stop all preview containers for a project.
    /// </summary>
    public async Task StopProjectPreviewAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = await _docker.GetClient().Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["label"] = new Dictionary<string, bool>
                        {
                            ["cronbot.preview=true"] = true,
                            [$"cronbot.project-id={projectId}"] = true
                        }
                    }
                },
                cancellationToken);

            foreach (var container in containers)
            {
                try
                {
                    await _docker.StopContainerAsync(container.ID, cancellationToken);
                    await _docker.RemoveContainerAsync(container.ID, force: true, cancellationToken);
                    _logger.LogInformation("Stopped existing preview container {ContainerId}", container.ID);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop existing preview {ContainerId}", container.ID);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping project previews");
        }
    }
}

/// <summary>
/// Result of starting a preview.
/// </summary>
public class PreviewResult
{
    public Guid PreviewId { get; set; }
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public int Port { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
}

/// <summary>
/// Result of taking a screenshot.
/// </summary>
public class ScreenshotResult
{
    public bool Success { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ScreenshotUrl { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
