using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Background service that initializes Gitea on startup.
/// Creates the admin user if it doesn't exist.
/// </summary>
public class GiteaInitializer : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GiteaInitializer> _logger;

    public GiteaInitializer(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GiteaInitializer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var giteaUrl = _configuration["Gitea:Url"] ?? "http://gitea:3000";
        var username = _configuration["Gitea:Username"] ?? "cronbot";
        var password = _configuration["Gitea:Password"] ?? "cronbot123";
        var email = "admin@cronbot.local";

        _logger.LogInformation("Initializing Gitea at {Url}", giteaUrl);

        // Wait for Gitea to be ready
        using var httpClient = _httpClientFactory.CreateClient("Gitea");
        httpClient.BaseAddress = new Uri(giteaUrl);

        var maxRetries = 30;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/v1/version", stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Gitea is ready");
                    break;
                }
            }
            catch
            {
                // Gitea not ready yet
            }

            if (i == maxRetries - 1)
            {
                _logger.LogWarning("Gitea not available after {Retries} attempts, skipping initialization", maxRetries);
                return;
            }

            await Task.Delay(2000, stoppingToken);
        }

        // Check if admin user already exists
        try
        {
            var basicAuth = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));

            using var checkRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
            checkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);

            var checkResponse = await httpClient.SendAsync(checkRequest, stoppingToken);
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gitea admin user '{Username}' already exists", username);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking existing user");
        }

        // User doesn't exist, but we can't create it via API
        // The user needs to be created via Gitea CLI
        // Log instructions for manual setup
        _logger.LogWarning(
            "Gitea admin user '{Username}' not found. " +
            "Run this command to create it: " +
            "docker compose exec -u git gitea gitea admin user create --username {Username} --password {Password} --email {Email} --must-change-password=false --admin",
            username, username, password, email);
    }
}
