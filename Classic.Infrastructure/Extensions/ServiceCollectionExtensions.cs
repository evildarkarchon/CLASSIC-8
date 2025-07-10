using Classic.Core.Interfaces;
using Classic.Core.Enums;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.Logging;
using Classic.Infrastructure.Messaging;
using Classic.Infrastructure.Registry;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Classic.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassicInfrastructure(this IServiceCollection services)
    {
        // Configure Serilog as the global logger
        Log.Logger = LoggingConfiguration.CreateLogger();

        // Register core services
        services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
        services.AddSingleton<IGlobalRegistry, GlobalRegistry>();

        // Register message handlers
        services.AddSingleton<ConsoleMessageHandler>();
        services.AddSingleton<GuiMessageHandler>();

        // Register factory for message handlers
        services.AddSingleton<Func<MessageTarget, IMessageHandler>>(provider => target =>
        {
            return target switch
            {
                MessageTarget.CLI => (IMessageHandler)provider.GetRequiredService<ConsoleMessageHandler>(),
                MessageTarget.GUI => (IMessageHandler)provider.GetRequiredService<GuiMessageHandler>(),
                MessageTarget.Both => (IMessageHandler)provider.GetRequiredService<ConsoleMessageHandler>(), // Default fallback
                _ => throw new ArgumentException($"Unknown message target: {target}")
            };
        });

        // Register default message handler as CLI
        services.AddSingleton<IMessageHandler>(provider =>
            provider.GetRequiredService<ConsoleMessageHandler>());

        // Register Serilog logger as singleton
        services.AddSingleton<Serilog.ILogger>(Log.Logger);

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
