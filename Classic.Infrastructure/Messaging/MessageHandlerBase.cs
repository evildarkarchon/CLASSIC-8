using Classic.Core.Interfaces;
using Classic.Core.Enums;
using Microsoft.Extensions.Logging;

namespace Classic.Infrastructure.Messaging;

public abstract class MessageHandlerBase : IMessageHandler
{
    protected readonly ILogger<MessageHandlerBase> Logger;

    protected MessageHandlerBase(ILogger<MessageHandlerBase> logger)
    {
        Logger = logger;
    }

    public abstract void SendMessage(string message, MessageType type, MessageTarget target);
    public abstract void ReportProgress(string operation, int current, int total);
    public abstract IDisposable BeginProgressContext(string operation, int total);
}
