using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Result of creating an agent container.
/// </summary>
public class ContainerCreationResult
{
    public string? ContainerId { get; set; }
    public string? ImageHash { get; set; }
}

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
    public async Task<ContainerCreationResult> CreateAgentContainerAsync(
        string name,
        Guid agentId,
        Guid projectId,
        int autonomyLevel,
        string apiKey,
        string? apiBaseUrl,
        string? model,
        int? maxTokens,
        string kanbanUrl,
        string? giteaUrl = null,
        string? giteaUsername = null,
        string? giteaPassword = null,
        string? giteaToken = null,
        string? repoUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"cronbot-agent-{agentId.ToString()[..8]}";
            var imageName = "cronbot-agent:latest";

            // Check if container already exists
            var existing = await GetContainerByNameAsync(containerName, cancellationToken);
            if (existing != null)
            {
                _logger.LogWarning("Container {ContainerName} already exists", containerName);
                return new ContainerCreationResult
                {
                    ContainerId = existing.ID,
                    ImageHash = existing.ImageID?.Replace("sha256:", "")?[..12] ?? "unknown"
                };
            }

            // Pull image first and get the image hash
            var imageHash = await EnsureImageExistsAsync("cronbot-agent", "latest", cancellationToken);

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
                "BLOCKER_WAIT_MS=30000",
                // Git/Gitea configuration for branching workflow
                $"GITEA_URL={giteaUrl ?? ""}",
                $"GITEA_USERNAME={giteaUsername ?? ""}",
                $"GITEA_PASSWORD={giteaPassword ?? ""}",
                $"GITEA_TOKEN={giteaToken ?? ""}",
                $"REPO_URL={repoUrl ?? ""}"
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
                Image = imageName,
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

            return new ContainerCreationResult
            {
                ContainerId = container.ID,
                ImageHash = imageHash
            };
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
    /// Restart a container.
    /// </summary>
    public async Task<bool> RestartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters
            {
                WaitBeforeKillSeconds = 10
            }, cancellationToken);

            _logger.LogInformation("Restarted container {ContainerId}", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart container {ContainerId}", containerId);
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
            // Use the newer API with demux = true for proper stream handling
            using var multiplexedStream = await _client.Containers.GetContainerLogsAsync(
                containerId,
                true, // demux stdout/stderr
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = tail.ToString()
                },
                cancellationToken);

            // Read from MultiplexedStream
            using var memStream = new MemoryStream();
            await multiplexedStream.CopyOutputToAsync(null, memStream, null, cancellationToken);
            memStream.Position = 0;
            using var reader = new StreamReader(memStream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Ensure an image exists locally. Build from source if not found, pull from registry as fallback.
    /// Returns the short image hash (first 12 characters).
    /// </summary>
    private async Task<string> EnsureImageExistsAsync(string image, string tag, CancellationToken cancellationToken)
    {
        try
        {
            // Check if image already exists locally
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [$"{image}:{tag}"] = true }
                }
            }, cancellationToken);

            if (images.Count > 0)
            {
                var existingHash = images[0].ID?.Replace("sha256:", "")?[..12] ?? "unknown";
                _logger.LogDebug("Image {Image}:{Tag} already exists locally (hash: {Hash})", image, tag, existingHash);
                return existingHash;
            }

            _logger.LogInformation("Image {Image}:{Tag} not found locally", image, tag);

            // Try to build from local source first
            var buildHash = await TryBuildImageAsync(image, tag, cancellationToken);
            if (!string.IsNullOrEmpty(buildHash))
            {
                _logger.LogInformation("Successfully built image {Image}:{Tag} (hash: {Hash})", image, tag, buildHash);
                return buildHash;
            }

            // Fallback: try to pull from registry
            _logger.LogInformation("Build failed, attempting to pull {Image}:{Tag} from registry", image, tag);
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

            // Get the hash of the pulled image
            images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [$"{image}:{tag}"] = true }
                }
            }, cancellationToken);

            var pulledHash = images.Count > 0
                ? images[0].ID?.Replace("sha256:", "")?[..12] ?? "unknown"
                : "unknown";

            _logger.LogInformation("Successfully pulled image {Image}:{Tag} (hash: {Hash})", image, tag, pulledHash);
            return pulledHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure image {Image}:{Tag} exists", image, tag);
            throw;
        }
    }

    /// <summary>
    /// Try to build the agent image from local source.
    /// Returns the image hash if successful, null otherwise.
    /// </summary>
    private async Task<string?> TryBuildImageAsync(string image, string tag, CancellationToken cancellationToken)
    {
        // Look for the agent source directory
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            "/app/agent-src",                    // When running in container with mounted source
            "./src/CronBot.Agent",               // Development: relative to API working dir
            "../CronBot.Agent",                  // Development: sibling directory
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CronBot.Agent")), // Project relative
        };

        string? buildContextPath = null;
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "Dockerfile")))
            {
                buildContextPath = fullPath;
                break;
            }
        }

        if (buildContextPath == null)
        {
            _logger.LogWarning("Could not find agent source directory for building. Tried: {Paths}", string.Join(", ", possiblePaths));
            return null;
        }

        _logger.LogInformation("Building image {Image}:{Tag} from {Path}", image, tag, buildContextPath);

        try
        {
            // Create a tar archive of the build context
            var tarPath = Path.Combine(Path.GetTempPath(), $"cronbot-agent-build-{Guid.NewGuid()}.tar");
            CreateTarFile(buildContextPath, tarPath);

            using var tarStream = File.OpenRead(tarPath);

            var buildParameters = new ImageBuildParameters
            {
                Dockerfile = "Dockerfile",
                Tags = new List<string> { $"{image}:{tag}" },
                Remove = true,
                ForceRemove = true,
                NoCache = false,
            };

            var buildOutput = new List<string>();
            var progress = new Progress<JSONMessage>(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Stream))
                {
                    var output = msg.Stream.Trim();
                    buildOutput.Add(output);
                    _logger.LogDebug("Build: {Output}", output);
                }
                if (msg.Error != null)
                {
                    _logger.LogError("Build error: {Error}", msg.Error.Message);
                }
            });

            await _client.Images.BuildImageFromDockerfileAsync(
                buildParameters,
                tarStream,
                null,
                null,
                progress,
                cancellationToken);

            // Cleanup tar file
            try { File.Delete(tarPath); } catch { /* ignore */ }

            // Verify image was created and get hash
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [$"{image}:{tag}"] = true }
                }
            }, cancellationToken);

            if (images.Count > 0)
            {
                return images[0].ID?.Replace("sha256:", "")?[..12] ?? "unknown";
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build image {Image}:{Tag}", image, tag);
            return null;
        }
    }

    /// <summary>
    /// Create a tar archive from a directory.
    /// </summary>
    private void CreateTarFile(string sourceDirectory, string tarPath)
    {
        // Use tar command (available on Linux/macOS, and in Docker containers)
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-C \"{sourceDirectory}\" -cf \"{tarPath}\" .",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start tar process");
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"tar failed with exit code {process.ExitCode}: {error}");
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

    /// <summary>
    /// Get the underlying Docker client for advanced operations.
    /// </summary>
    public Docker.DotNet.DockerClient GetClient() => _client;

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
