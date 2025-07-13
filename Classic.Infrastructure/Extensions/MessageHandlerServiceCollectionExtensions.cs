using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering message handlers with a cleaner configuration API.
/// </summary>
public static class MessageHandlerServiceCollectionExtensions
{
    /// <summary>
    /// Adds message handlers to the service collection with fluent configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure message handler options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageHandlers(
        this IServiceCollection services,
        Action<MessageHandlerOptions> configure)
    {
        var options = new MessageHandlerOptions();
        configure(options);

        // Register the options as a singleton
        services.AddSingleton(options);

        // Register each handler type as a singleton
        foreach (var handlerType in options.HandlerMappings.Values.Distinct())
        {
            services.AddSingleton(handlerType);
        }

        // Register factory for resolving handlers by target
        services.AddSingleton<Func<MessageTarget, IMessageHandler>>(provider => target =>
        {
            var handlerOptions = provider.GetRequiredService<MessageHandlerOptions>();
            var handlerType = handlerOptions.GetHandlerType(target);
            return (IMessageHandler)provider.GetRequiredService(handlerType);
        });

        // Register the default message handler based on configured default target
        services.AddSingleton<IMessageHandler>(provider =>
        {
            var handlerOptions = provider.GetRequiredService<MessageHandlerOptions>();
            var factory = provider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();
            return factory(handlerOptions.DefaultTarget);
        });

        return services;
    }

    /// <summary>
    /// Adds message handlers with default configuration (CLI and GUI handlers with CLI as default).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDefaultMessageHandlers(this IServiceCollection services)
    {
        return services.AddMessageHandlers(options =>
        {
            options.DefaultTarget = MessageTarget.Cli;
            options.RegisterHandler<ConsoleMessageHandler>(MessageTarget.Cli);
            options.RegisterHandler<GuiMessageHandler>(MessageTarget.Gui);
        });
    }
}
