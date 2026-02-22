using System.ComponentModel;
using System.Text.Json;
using CronBot.Infrastructure.Services;
using ModelContextProtocol.Server;

namespace CronBot.Api.Mcp;

/// <summary>
/// MCP tools for preview operations.
/// </summary>
[McpServerToolType]
public static class PreviewTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Start a preview container for a project.
    /// </summary>
    [McpServerTool, Description(
        "Start a preview of the project application. " +
        "This spins up a container running the project's code so you can verify it visually. " +
        "Returns the preview URL and container information.")]
    public static async Task<string> StartPreview(
        PreviewService previewService,
        [Description("The project ID")] string projectId,
        [Description("Port to expose the application on (default: 3000)")] int port = 3000,
        [Description("Optional command to run instead of npm start")] string? command = null)
    {
        if (!Guid.TryParse(projectId, out var projectIdGuid))
        {
            return JsonSerializer.Serialize(new { error = "Invalid project ID format" }, JsonOptions);
        }

        var result = await previewService.StartPreviewAsync(projectIdGuid, port, command);

        if (result == null)
        {
            return JsonSerializer.Serialize(new
            {
                error = "Failed to start preview",
                message = "No active agent found for this project, or container creation failed"
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            previewId = result.PreviewId,
            containerId = result.ContainerId,
            containerName = result.ContainerName,
            projectId = result.ProjectId,
            port = result.Port,
            url = result.Url,
            startedAt = result.StartedAt,
            message = $"Preview started at {result.Url}. Wait a few seconds for the app to initialize."
        }, JsonOptions);
    }

    /// <summary>
    /// Take a screenshot of a running preview.
    /// </summary>
    [McpServerTool, Description(
        "Take a screenshot of a running preview to verify the application visually. " +
        "Returns the screenshot URL or status message.")]
    public static async Task<string> TakeScreenshot(
        PreviewService previewService,
        [Description("The preview URL to screenshot")] string previewUrl,
        [Description("CSS selector to wait for before taking screenshot")] string? waitForSelector = null,
        [Description("Viewport width in pixels (default: 1280)")] int width = 1280,
        [Description("Viewport height in pixels (default: 720)")] int height = 720,
        [Description("Milliseconds to wait for page load (default: 2000)")] int waitForMs = 2000)
    {
        var result = await previewService.TakeScreenshotAsync(
            previewUrl,
            waitForSelector,
            width,
            height,
            waitForMs);

        if (result == null)
        {
            return JsonSerializer.Serialize(new
            {
                error = "Failed to take screenshot",
                message = "Screenshot service unavailable"
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            success = result.Success,
            url = result.Url,
            message = result.Message,
            screenshotUrl = result.ScreenshotUrl,
            capturedAt = result.CapturedAt
        }, JsonOptions);
    }

    /// <summary>
    /// Stop a running preview container.
    /// </summary>
    [McpServerTool, Description(
        "Stop and remove a preview container. " +
        "Use this when you're done verifying the application to free up resources.")]
    public static async Task<string> StopPreview(
        PreviewService previewService,
        [Description("The container ID of the preview to stop")] string containerId)
    {
        var success = await previewService.StopPreviewAsync(containerId);

        return JsonSerializer.Serialize(new
        {
            success,
            containerId,
            message = success
                ? "Preview container stopped and removed"
                : "Failed to stop preview container"
        }, JsonOptions);
    }

    /// <summary>
    /// Check if a preview URL is accessible.
    /// </summary>
    [McpServerTool, Description(
        "Check if a preview URL is accessible and returning a successful response. " +
        "Use this to verify the app is running before taking a screenshot.")]
    public static async Task<string> CheckPreview(
        PreviewService previewService,
        [Description("The preview URL to check")] string previewUrl)
    {
        var result = await previewService.TakeScreenshotAsync(previewUrl, waitForMs: 1000);

        return JsonSerializer.Serialize(new
        {
            accessible = result?.Success ?? false,
            url = previewUrl,
            message = result?.Message ?? "Unable to reach preview"
        }, JsonOptions);
    }
}
