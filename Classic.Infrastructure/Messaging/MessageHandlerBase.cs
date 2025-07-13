using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public abstract class MessageHandlerBase : IMessageHandler
{
    protected readonly ILogger Logger;

    protected MessageHandlerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract void SendMessage(string message, MessageType type, MessageTarget target);
    public abstract void ReportProgress(string operation, int current, int total);
    public abstract IDisposable BeginProgressContext(string operation, int total);

    // Async methods - each implementation should provide appropriate async behavior
    public abstract Task SendMessageAsync(string message, CancellationToken cancellationToken = default);

    public abstract Task SendProgressAsync(int current, int total, string message,
        CancellationToken cancellationToken = default);
}
