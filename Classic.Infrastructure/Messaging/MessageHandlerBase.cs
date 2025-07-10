using Classic.Core.Interfaces;
using Classic.Core.Enums;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public abstract class MessageHandlerBase(ILogger logger) : IMessageHandler
{
    protected readonly ILogger Logger = logger;

    public abstract void SendMessage(string message, MessageType type, MessageTarget target);
    public abstract void ReportProgress(string operation, int current, int total);
    public abstract IDisposable BeginProgressContext(string operation, int total);
}
