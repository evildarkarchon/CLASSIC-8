using System.Collections.Concurrent;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public class GuiMessageHandler : MessageHandlerBase
{
    private readonly ConcurrentQueue<GuiMessage> _messageQueue = new();
    private readonly IMessageFormattingService _formattingService;

    public GuiMessageHandler(ILogger logger, IMessageFormattingService formattingService)
        : base(logger)
    {
        _formattingService = formattingService ?? throw new ArgumentNullException(nameof(formattingService));
    }

    public event EventHandler<GuiMessage>? MessageReceived;
    public event EventHandler<ProgressUpdate>? ProgressUpdated;

    public override void SendMessage(string message, MessageType type, MessageTarget target)
    {
        if (!target.HasFlag(MessageTarget.Gui)) return;

        var guiMessage = new GuiMessage
        {
            Message = message,
            Type = type,
            Timestamp = DateTime.Now,
            FormattedMessage = _formattingService.FormatMessage(message, type),
            Icon = _formattingService.GetMessageIcon(type)
        };

        _messageQueue.Enqueue(guiMessage);
        MessageReceived?.Invoke(this, guiMessage);

        Logger.Information("GUI Message sent: [{Type}] {Message}", type, message);
    }

    public override void ReportProgress(string operation, int current, int total)
    {
        var progressUpdate = new ProgressUpdate
        {
            Operation = operation,
            Current = current,
            Total = total,
            Percentage = total > 0 ? (double)current / total * 100 : 0,
            IsCompleted = current >= total
        };

        ProgressUpdated?.Invoke(this, progressUpdate);
    }

    public override IDisposable BeginProgressContext(string operation, int total)
    {
        return new ProgressContext(this, operation, total);
    }

    public IEnumerable<GuiMessage> GetQueuedMessages()
    {
        var messages = new List<GuiMessage>();
        while (_messageQueue.TryDequeue(out var message)) messages.Add(message);
        return messages;
    }

    public void ClearMessageQueue()
    {
        while (_messageQueue.TryDequeue(out _)) { }
    }
}

public class GuiMessage
{
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string FormattedMessage { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ProgressUpdate
{
    public string Operation { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public double Percentage { get; set; }
    public bool IsCompleted { get; set; }
}
