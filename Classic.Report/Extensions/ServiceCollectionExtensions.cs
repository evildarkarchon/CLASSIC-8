using Classic.Report.Factories;
using Classic.Report.Generators;
using Classic.Report.Interfaces;
using Classic.Report.Services;
using Classic.Report.Strategies;
using Classic.Report.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Classic.Report.Extensions;

/// <summary>
/// Extension methods for registering Classic.Report services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the unified report generation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClassicReporting(this IServiceCollection services)
    {
        // Register the unified report generator
        services.AddScoped<IUnifiedReportGenerator, UnifiedReportGenerator>();

        // Register the strategy factory
        services.AddSingleton<IReportStrategyFactory, ReportStrategyFactory>();

        // Register template selection service
        services.AddScoped<TemplateSelectionService>();

        // Register all report strategies
        services.AddScoped<IReportStrategy, StandardReportStrategy>();
        services.AddScoped<IReportStrategy, EnhancedReportStrategy>();
        services.AddScoped<IReportStrategy, AdvancedReportStrategy>();

        // Register section generators (placeholders for now)
        RegisterSectionGenerators(services);

        return services;
    }

    /// <summary>
    /// Adds report generation services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Configuration action for report options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClassicReporting(
        this IServiceCollection services,
        Action<ReportingConfiguration> configure)
    {
        var config = new ReportingConfiguration();
        configure(config);

        services.AddSingleton(config);

        return AddClassicReporting(services);
    }

    private static void RegisterSectionGenerators(IServiceCollection services)
    {
        // Register placeholder section generators
        // These will be replaced with actual implementations in later phases
        services.AddScoped<IHeaderSectionGenerator, PlaceholderHeaderSectionGenerator>();
        services.AddScoped<IErrorSectionGenerator, PlaceholderErrorSectionGenerator>();
        services.AddScoped<ISuspectSectionGenerator, PlaceholderSuspectSectionGenerator>();
        services.AddScoped<ISettingsValidationSectionGenerator, PlaceholderSettingsValidationSectionGenerator>();
        services.AddScoped<IPluginSectionGenerator, PlaceholderPluginSectionGenerator>();
        services.AddScoped<IFormIdSectionGenerator, PlaceholderFormIdSectionGenerator>();
        services.AddScoped<INamedRecordSectionGenerator, PlaceholderNamedRecordSectionGenerator>();
        services.AddScoped<IFooterSectionGenerator, PlaceholderFooterSectionGenerator>();

        // Enhanced section generators (optional)
        services.AddScoped<IExecutiveSummarySectionGenerator, PlaceholderExecutiveSummarySectionGenerator>();
        services.AddScoped<IPerformanceMetricsSectionGenerator, PlaceholderPerformanceMetricsSectionGenerator>();
        services.AddScoped<IGameHintsSectionGenerator, PlaceholderGameHintsSectionGenerator>();
    }
}

/// <summary>
/// Configuration options for the reporting system.
/// </summary>
public class ReportingConfiguration
{
    /// <summary>
    /// Gets or sets the default report options.
    /// </summary>
    public ReportOptions DefaultOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable detailed logging for report generation.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the threshold for automatically selecting enhanced format.
    /// </summary>
    public int EnhancedFormatThreshold { get; set; } = 3;
}
