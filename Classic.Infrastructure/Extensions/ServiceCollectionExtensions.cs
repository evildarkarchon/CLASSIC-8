using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.GameManagement;
using Classic.Infrastructure.Logging;
using Classic.Infrastructure.Messaging;
using Classic.Infrastructure.Reporting;
using Classic.Infrastructure.Reporting.Templates;
using Classic.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO.Abstractions;

namespace Classic.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassicInfrastructure(this IServiceCollection services)
    {
        // Configure Serilog as the global logger
        Log.Logger = LoggingConfiguration.CreateLogger();

        // Register core services
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IGameConfiguration, GameConfiguration>();

        // Register consolidated settings system - ISettingsService is the single public interface
        services.AddSingleton<YamlSettings>();
        services.AddSingleton<IYamlSettingsProvider>(provider => provider.GetRequiredService<YamlSettings>());
        services.AddSingleton<ISettingsService, SettingsService>();

        // Keep legacy cache for backward compatibility during transition
        // Only register if explicitly needed for existing legacy code
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
#pragma warning restore CS0618 // Type or member is obsolete

        // Register game file management
        services.AddScoped<IGameFileManager, GameFileManager>();

        // Register progress and notification services
        services.AddSingleton<IProgressService, ProgressService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Register HTTP client for update services
        services.AddHttpClient<IGitHubApiService, GitHubApiService>();
        services.AddHttpClient<INexusModsService, NexusModsService>();

        // Register update services
        services.AddSingleton<IVersionService, VersionService>();
        services.AddScoped<IUpdateService, UpdateService>();

        // Register Papyrus and Pastebin services
        services.AddSingleton<IPapyrusMonitoringService, PapyrusMonitoringService>();
        services.AddHttpClient<IPastebinService, PastebinService>();

        // Register message formatting service
        services.AddSingleton<IMessageFormattingService, MessageFormattingService>();

        // Register message handlers using the new cleaner configuration pattern
        services.AddMessageHandlers(options =>
        {
            options.DefaultTarget = MessageTarget.Cli;
            options.RegisterHandler<ConsoleMessageHandler>(MessageTarget.Cli);
            options.RegisterHandler<GuiMessageHandler>(MessageTarget.Gui);
            // Note: MessageTarget.Both will fall back to the default (CLI) handler
        });

        // Register reporting services
        services.AddScoped<IReportTemplate, MarkdownReportTemplate>();
        services.AddScoped<IReportGenerator, ReportGenerator>();

        // Register Serilog logger as singleton
        services.AddSingleton(Log.Logger);

        return services;
    }

    public static IServiceCollection AddClassicCore(this IServiceCollection services)
    {
        // This method is for registering core domain services
        // Currently placeholder for future scan orchestrator implementation

        // TODO: Add scan orchestrator when implemented
        // services.AddScoped<IScanOrchestrator, AsyncScanOrchestrator>();

        // TODO: Add analyzers when implemented
        // services.AddTransient<IFormIDAnalyzer, FormIDAnalyzer>();
        // services.AddTransient<IPluginAnalyzer, PluginAnalyzer>();
        // services.AddTransient<ICrashLogParser, CrashLogParser>();

        return services;
    }
}
