using Classic.Core.Enums;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public class ConsoleMessageHandler(ILogger logger) : MessageHandlerBase(logger)
{
    public override void SendMessage(string message, MessageType type, MessageTarget target)
    {
        if (!target.HasFlag(MessageTarget.CLI)) return;

        var color = type switch
        {
            MessageType.Error => ConsoleColor.Red,
            MessageType.Critical => ConsoleColor.DarkRed,
            MessageType.Warning => ConsoleColor.Yellow,
            MessageType.Success => ConsoleColor.Green,
            MessageType.Debug => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"[{type}] {message}");
        Console.ResetColor();

        Logger.Information("Message sent: [{Type}] {Message}", type, message);
    }

    public override void ReportProgress(string operation, int current, int total)
    {
        var percentage = (double)current / total * 100;
        Console.Write($"\r{operation}: {current}/{total} ({percentage:F1}%)");

        if (current == total)
        {
            Console.WriteLine(); // New line when complete
        }
    }

    public override IDisposable BeginProgressContext(string operation, int total)
    {
        return new ProgressContext(this, operation, total);
    }

    private class ProgressContext : IDisposable
    {
        private readonly ConsoleMessageHandler _handler;
        private readonly string _operation;
        private readonly int _total;
        private int _current;

        public ProgressContext(ConsoleMessageHandler handler, string operation, int total)
        {
            _handler = handler;
            _operation = operation;
            _total = total;
            _current = 0;

            Console.WriteLine($"Starting: {operation}");
        }

        public void Increment()
        {
            _current++;
            _handler.ReportProgress(_operation, _current, _total);
        }

        public void Dispose()
        {
            if (_current < _total)
            {
                _handler.ReportProgress(_operation, _total, _total);
            }
            Console.WriteLine($"Completed: {_operation}");
        }
    }
}
