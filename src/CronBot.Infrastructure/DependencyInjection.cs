using CronBot.Infrastructure.Data;
using CronBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronBot.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Register Docker client as singleton
        services.AddSingleton<DockerClientService>();

        // Register Orchestrator service
        services.AddScoped<OrchestratorService>();

        // Register Git service for Gitea integration
        services.AddHttpClient("Gitea");
        services.AddScoped<GitService>();

        // Register Preview service
        services.AddScoped<PreviewService>();

        return services;
    }
}
