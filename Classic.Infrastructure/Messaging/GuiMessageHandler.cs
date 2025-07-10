using System.Collections.Concurrent;
using Classic.Core.Enums;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public class GuiMessageHandler(ILogger logger) : MessageHandlerBase(logger)
{
    private readonly ConcurrentQueue<GuiMessage> _messageQueue = new();

    public event EventHandler<GuiMessage>? MessageReceived;
    public event EventHandler<ProgressUpdate>? ProgressUpdated;

    public override void SendMessage(string message, MessageType type, MessageTarget target)
    {
        if (!target.HasFlag(MessageTarget.Gui)) return;

        var guiMessage = new GuiMessage
        {
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
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
        return new GuiProgressContext(this, operation, total);
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

    private class GuiProgressContext : IDisposable
    {
        private readonly GuiMessageHandler _handler;
        private readonly string _operation;
        private readonly int _total;
        private int _current;

        public GuiProgressContext(GuiMessageHandler handler, string operation, int total)
        {
            _handler = handler;
            _operation = operation;
            _total = total;
            _current = 0;

            _handler.ReportProgress(_operation, _current, _total);
        }

        public void Dispose()
        {
            if (_current < _total) _handler.ReportProgress(_operation, _total, _total);
        }

        public void Increment()
        {
            _current++;
            _handler.ReportProgress(_operation, _current, _total);
        }
    }
}

public class GuiMessage
{
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ProgressUpdate
{
    public string Operation { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public double Percentage { get; set; }
    public bool IsCompleted { get; set; }
}
