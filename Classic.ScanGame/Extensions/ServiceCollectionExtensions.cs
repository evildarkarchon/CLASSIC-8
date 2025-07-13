using Classic.Core.Interfaces;
using Classic.ScanGame.Checkers;
using Classic.ScanGame.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.ScanGame.Extensions;

/// <summary>
/// Extension methods for registering ScanGame services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all ScanGame services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddScanGameServices(this IServiceCollection services)
    {
        // Register checkers with their interfaces
        services.AddScoped<IXsePluginChecker, XsePluginChecker>();
        services.AddScoped<ICrashgenChecker, CrashgenChecker>();
        services.AddScoped<IWryeBashChecker, WryeBashChecker>();
        services.AddScoped<IModIniScanner, ModIniScanner>();

        // Register concrete implementations for direct injection
        services.AddScoped<XsePluginChecker>();
        services.AddScoped<CrashgenChecker>();
        services.AddScoped<WryeBashChecker>();
        services.AddScoped<ModIniScanner>();

        // Register configuration managers
        services.AddSingleton<IniConfigurationManager>();
        services.AddSingleton<TomlConfigurationManager>();

        // Register orchestrator
        services.AddScoped<GameScanOrchestrator>();

        return services;
    }
}
