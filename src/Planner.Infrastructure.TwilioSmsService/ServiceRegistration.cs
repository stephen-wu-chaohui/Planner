using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planner.Application;

namespace Planner.Infrastructure.TwilioSmsService;

/// <summary>
/// Extension methods for registering Twilio SMS service.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers the Twilio SMS service and its dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<TwilioSmsOptions>(
            configuration.GetSection(TwilioSmsOptions.SectionName));

        // Register the SMS service
        services.AddScoped<ISmsService, TwilioSmsService>();

        return services;
    }
}
