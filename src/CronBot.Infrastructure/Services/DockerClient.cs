using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Client for interacting with Docker API.
/// </summary>
public class DockerClientService : IDisposable
{
    private readonly Docker.DotNet.DockerClient _client;
    private readonly ILogger<DockerClientService> _logger;
    private bool _disposed;

    public DockerClientService(IConfiguration configuration, ILogger<DockerClientService> logger)
    {
        _logger = logger;

        // Connect to Docker socket
        var dockerHost = configuration["Docker:Host"] ?? "unix:///var/run/docker.sock";
        _client = new DockerClientConfiguration(new Uri(dockerHost)).CreateClient();
    }

    /// <summary>
    /// Create and start a container for an agent.
    /// </summary>
    public async Task<string?> CreateAgentContainerAsync(
        string name,
        Guid agentId,
        Guid projectId,
        int autonomyLevel,
        string apiKey,
        string? apiBaseUrl,
        string? model,
        int? maxTokens,
        string kanbanUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"cronbot-agent-{agentId.ToString()[..8]}";

            // Check if container already exists
            var existing = await GetContainerByNameAsync(containerName, cancellationToken);
            if (existing != null)
            {
                _logger.LogWarning("Container {ContainerName} already exists", containerName);
                return existing.ID;
            }

            // Pull image first
            await EnsureImageExistsAsync("cronbot-agent", "latest", cancellationToken);

            var environment = new List<string>
            {
                $"PROJECT_ID={projectId}",
                $"AGENT_ID={agentId}",
                $"AUTONOMY_LEVEL={autonomyLevel}",
                $"ANTHROPIC_API_KEY={apiKey}",
                $"ANTHROPIC_BASE_URL={apiBaseUrl ?? ""}",
                $"ANTHROPIC_MODEL={model ?? "claude-sonnet-4-20250514"}",
                $"ANTHROPIC_MAX_TOKENS={maxTokens ?? 4096}",
                $"KANBAN_URL={kanbanUrl}",
                "WORKSPACE_PATH=/workspace",
                "STATE_PATH=/agent-state",
                "TASK_POLL_INTERVAL_MS=30000",
                "RETRY_DELAY_MS=10000",
                "BLOCKER_WAIT_MS=30000"
            };

            var labels = new Dictionary<string, string>
            {
                ["cronbot.managed"] = "true",
                ["cronbot.project-id"] = projectId.ToString(),
                ["cronbot.agent-id"] = agentId.ToString()
            };

            var createParams = new CreateContainerParameters
            {
                Name = containerName,
                Image = "cronbot-agent:latest",
                Env = environment,
                Labels = labels,
                HostConfig = new HostConfig
                {
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.UnlessStopped
                    },
                    Binds = new List<string>
                    {
                        $"agent-{agentId}-workspace:/workspace",
                        $"agent-{agentId}-state:/agent-state"
                    },
                    NetworkMode = "cronbot_default"
                },
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        ["cronbot_default"] = new()
                    }
                }
            };

            var container = await _client.Containers.CreateContainerAsync(createParams, cancellationToken);

            // Start the container
            await _client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancellationToken);

            _logger.LogInformation("Created and started container {ContainerName} for agent {AgentId}", containerName, agentId);

            return container.ID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container for agent {AgentId}", agentId);
            throw;
        }
    }

    /// <summary>
    /// Get container by name.
    /// </summary>
    public async Task<ContainerListResponse?> GetContainerByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [name] = true }
                }
            }, cancellationToken);

            return containers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container {ContainerName}", name);
            return null;
        }
    }

    /// <summary>
    /// Get container by ID.
    /// </summary>
    public async Task<ContainerInspectResponse?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.Containers.InspectContainerAsync(containerId, cancellationToken);
        }
        catch (DockerContainerNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inspect container {ContainerId}", containerId);
            return null;
        }
    }

    /// <summary>
    /// Stop a container.
    /// </summary>
    public async Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters
            {
                WaitBeforeKillSeconds = 10
            }, cancellationToken);

            _logger.LogInformation("Stopped container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            return false;
        }
    }

    /// <summary>
    /// Start a container.
    /// </summary>
    public async Task<bool> StartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
            _logger.LogInformation("Started container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerId}", containerId);
            return false;
        }
    }

    /// <summary>
    /// Remove a container.
    /// </summary>
    public async Task<bool> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = force,
                RemoveVolumes = true
            }, cancellationToken);

            _logger.LogInformation("Removed container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    /// <summary>
    /// Get container logs.
    /// </summary>
    public async Task<string> GetContainerLogsAsync(string containerId, int tail = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the older API with demux = true for proper stream handling
            using var stream = await _client.Containers.GetContainerLogsAsync(
                containerId,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = tail.ToString()
                },
                cancellationToken);

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Ensure an image exists locally, pull if needed.
    /// </summary>
    private async Task EnsureImageExistsAsync(string image, string tag, CancellationToken cancellationToken)
    {
        try
        {
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [$"{image}:{tag}"] = true }
                }
            }, cancellationToken);

            if (images.Count == 0)
            {
                _logger.LogInformation("Pulling image {Image}:{Tag}", image, tag);
                await _client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = image, Tag = tag },
                    null,
                    new Progress<JSONMessage>(msg =>
                    {
                        if (!string.IsNullOrEmpty(msg.Status))
                        {
                            _logger.LogDebug("Pull progress: {Status}", msg.Status);
                        }
                    }),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure image {Image}:{Tag} exists", image, tag);
            throw;
        }
    }

    /// <summary>
    /// Create volumes for an agent.
    /// </summary>
    public async Task CreateAgentVolumesAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaceVolume = $"agent-{agentId}-workspace";
            var stateVolume = $"agent-{agentId}-state";

            foreach (var volume in new[] { workspaceVolume, stateVolume })
            {
                try
                {
                    await _client.Volumes.CreateAsync(new VolumesCreateParameters
                    {
                        Name = volume,
                        Labels = new Dictionary<string, string>
                        {
                            ["cronbot.managed"] = "true",
                            ["cronbot.agent-id"] = agentId.ToString()
                        }
                    }, cancellationToken);

                    _logger.LogDebug("Created volume {VolumeName}", volume);
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // Volume already exists, that's fine
                    _logger.LogDebug("Volume {VolumeName} already exists", volume);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create volumes for agent {AgentId}", agentId);
        }
    }

    /// <summary>
    /// Ensure the cronbot_default network exists.
    /// </summary>
    public async Task EnsureNetworkExistsAsync(CancellationToken cancellationToken = default)
    {
        const string networkName = "cronbot_default";

        try
        {
            var networks = await _client.Networks.ListNetworksAsync(new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [networkName] = true }
                }
            }, cancellationToken);

            if (networks.Count == 0)
            {
                await _client.Networks.CreateNetworkAsync(new NetworksCreateParameters
                {
                    Name = networkName,
                    Labels = new Dictionary<string, string>
                    {
                        ["cronbot.managed"] = "true"
                    }
                }, cancellationToken);

                _logger.LogInformation("Created network {NetworkName}", networkName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure network {NetworkName} exists", networkName);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
