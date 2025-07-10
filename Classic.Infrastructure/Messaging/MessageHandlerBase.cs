using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public abstract class MessageHandlerBase : IMessageHandler
{
    protected readonly ILogger Logger;

    protected MessageHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract void SendMessage(string message, MessageType type, MessageTarget target);
    public abstract void ReportProgress(string operation, int current, int total);
    public abstract IDisposable BeginProgressContext(string operation, int total);

    public virtual Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        // Default implementation - subclasses should override
        Logger.Information("Message: {Message}", message);
        return Task.CompletedTask;
    }

    public virtual Task SendProgressAsync(int current, int total, string message,
        CancellationToken cancellationToken = default)
    {
        // Default implementation - subclasses should override
        Logger.Information("Progress: {Current}/{Total} - {Message}", current, total, message);
        return Task.CompletedTask;
    }
}
