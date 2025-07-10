using Classic.Core.Interfaces;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.Logging;
using Classic.Infrastructure.Messaging;
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
        
        // Register message handlers
        services.AddSingleton<IMessageHandler, ConsoleMessageHandler>();
        
        // Register Serilog logger as singleton
        services.AddSingleton<ILogger>(Log.Logger);
        
        return services;
    }
    
    public static IServiceCollection AddClassicCore(this IServiceCollection services)
    {
        // This method is for registering core domain services
        // Currently placeholder for future scan orchestrator implementation
        
        return services;
    }
}
