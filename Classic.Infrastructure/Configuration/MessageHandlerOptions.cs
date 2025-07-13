using Classic.Core.Enums;
using Classic.Core.Interfaces;

namespace Classic.Infrastructure.Configuration;

/// <summary>
/// Configuration options for message handler registration.
/// </summary>
public class MessageHandlerOptions
{
    /// <summary>
    /// Gets or sets the default message target when no specific target is provided.
    /// </summary>
    public MessageTarget DefaultTarget { get; set; } = MessageTarget.Cli;

    /// <summary>
    /// Dictionary mapping message targets to their handler types.
    /// </summary>
    internal Dictionary<MessageTarget, Type> HandlerMappings { get; } = new();

    /// <summary>
    /// Registers a message handler type for a specific target.
    /// </summary>
    /// <typeparam name="THandler">The message handler implementation type</typeparam>
    /// <param name="target">The message target this handler serves</param>
    /// <returns>The options instance for fluent configuration</returns>
    public MessageHandlerOptions RegisterHandler<THandler>(MessageTarget target)
        where THandler : class, IMessageHandler
    {
        HandlerMappings[target] = typeof(THandler);
        return this;
    }

    /// <summary>
    /// Gets the handler type for a specific target, falling back to default if not found.
    /// </summary>
    /// <param name="target">The target to get handler for</param>
    /// <returns>The handler type</returns>
    internal Type GetHandlerType(MessageTarget target)
    {
        if (HandlerMappings.TryGetValue(target, out var handlerType))
        {
            return handlerType;
        }

        // Fall back to default target if specific target not found
        if (target != DefaultTarget && HandlerMappings.TryGetValue(DefaultTarget, out var defaultHandlerType))
        {
            return defaultHandlerType;
        }

        throw new InvalidOperationException(
            $"No message handler registered for target '{target}' and no default handler available.");
    }
}
