using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.GameManagement;
using Classic.Infrastructure.Logging;
using Classic.Infrastructure.Messaging;
using Classic.Infrastructure.Reporting;
using Classic.Infrastructure.Reporting.Templates;
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
        services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
        services.AddSingleton<IGameConfiguration, GameConfiguration>();
        
        // Register new YAML settings system
        services.AddSingleton<IYamlSettings, YamlSettings>();
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // Register game file management
        services.AddScoped<IGameFileManager, GameFileManager>();
        
        // Register progress and notification services
        services.AddSingleton<IProgressService, Services.ProgressService>();
        services.AddSingleton<IAudioService, Services.AudioService>();
        services.AddSingleton<INotificationService, Services.NotificationService>();

        // Register message handlers
        services.AddSingleton<ConsoleMessageHandler>();
        services.AddSingleton<GuiMessageHandler>();

        // Register factory for message handlers
        services.AddSingleton<Func<MessageTarget, IMessageHandler>>(provider => target =>
        {
            return target switch
            {
                MessageTarget.Cli => provider.GetRequiredService<ConsoleMessageHandler>(),
                MessageTarget.Gui => provider.GetRequiredService<GuiMessageHandler>(),
                MessageTarget.Both => provider.GetRequiredService<ConsoleMessageHandler>(), // Default fallback
                _ => throw new ArgumentException($"Unknown message target: {target}")
            };
        });

        // Register default message handler as CLI
        services.AddSingleton<IMessageHandler>(provider =>
            provider.GetRequiredService<ConsoleMessageHandler>());

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
