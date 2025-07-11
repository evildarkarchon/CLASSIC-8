using Classic.Core.Interfaces;
using Classic.ScanLog.Analyzers;
using Classic.ScanLog.Models;
using Classic.ScanLog.Orchestration;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.ScanLog.Extensions;

/// <summary>
///     Extension methods for registering ScanLog services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds all ScanLog services to the DI container
    /// </summary>
    public static IServiceCollection AddScanLogServices(
        this IServiceCollection services,
        ScanLogConfiguration? configuration = null)
    {
        // Use default configuration if none provided
        configuration ??= new ScanLogConfiguration();

        // Register configuration as singleton
        services.AddSingleton(configuration);

        // Register cache as singleton
        services.AddSingleton<ConcurrentLogCache>(provider =>
            new ConcurrentLogCache(configuration.CacheTimeout));

        // Register parsers
        services.AddScoped<ICrashLogParser, CrashLogParser>();

        // Register analyzers
        services.AddScoped<IPluginAnalyzer, PluginAnalyzer>();
        services.AddScoped<IFormIdAnalyzer, FormIdAnalyzer>();
        services.AddScoped<SuspectScanner>();

        // Register orchestrator
        services.AddScoped<IScanOrchestrator, ComprehensiveScanOrchestrator>();

        return services;
    }

    /// <summary>
    ///     Adds ScanLog services with a custom configuration builder
    /// </summary>
    public static IServiceCollection AddScanLogServices(
        this IServiceCollection services,
        Action<ScanLogConfiguration> configureOptions)
    {
        var configuration = new ScanLogConfiguration();
        configureOptions(configuration);

        return services.AddScanLogServices(configuration);
    }

    /// <summary>
    ///     Adds just the core parsing services without full orchestration
    /// </summary>
    public static IServiceCollection AddScanLogParsers(
        this IServiceCollection services,
        ScanLogConfiguration? configuration = null)
    {
        configuration ??= new ScanLogConfiguration();

        services.AddSingleton(configuration);
        services.AddScoped<ICrashLogParser, CrashLogParser>();

        return services;
    }

    /// <summary>
    ///     Adds just the analysis services without parsing or orchestration
    /// </summary>
    public static IServiceCollection AddScanLogAnalyzers(
        this IServiceCollection services,
        ScanLogConfiguration? configuration = null)
    {
        configuration ??= new ScanLogConfiguration();

        services.AddSingleton(configuration);
        services.AddScoped<IPluginAnalyzer, PluginAnalyzer>(); // Keep this line as it is
        services.AddScoped<IFormIdAnalyzer, FormIdAnalyzer>();
        services.AddScoped<SuspectScanner>();

        return services;
    }
}
