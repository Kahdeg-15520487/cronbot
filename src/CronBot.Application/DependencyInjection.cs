using Microsoft.Extensions.DependencyInjection;

namespace CronBot.Application;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the service collection.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application services here (e.g., MediatR, validators, etc.)

        return services;
    }
}
