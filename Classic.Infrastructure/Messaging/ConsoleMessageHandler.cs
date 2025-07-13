using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Messaging;

public class ConsoleMessageHandler : MessageHandlerBase
{
    private readonly IMessageFormattingService _formattingService;

    public ConsoleMessageHandler(ILogger logger, IMessageFormattingService formattingService)
        : base(logger)
    {
        _formattingService = formattingService ?? throw new ArgumentNullException(nameof(formattingService));
    }

    public override void SendMessage(string message, MessageType type, MessageTarget target)
    {
        if (!target.HasFlag(MessageTarget.Cli)) return;

        var color = _formattingService.GetConsoleColor(type);
        var formattedMessage = _formattingService.FormatMessage(message, type);

        Console.ForegroundColor = color;
        Console.WriteLine(formattedMessage);
        Console.ResetColor();

        Logger.Information("Message sent: [{Type}] {Message}", type, message);
    }

    public override void ReportProgress(string operation, int current, int total)
    {
        var percentage = (double)current / total * 100;
        Console.Write($"\r{operation}: {current}/{total} ({percentage:F1}%)");

        if (current == total) Console.WriteLine(); // New line when complete
    }

    public override IDisposable BeginProgressContext(string operation, int total)
    {
        return new ProgressContext(this, operation, total);
    }

    // Async method implementations
    public override Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        SendMessage(message, MessageType.Info, MessageTarget.Cli);
        return Task.CompletedTask;
    }

    public override Task SendProgressAsync(int current, int total, string message,
        CancellationToken cancellationToken = default)
    {
        ReportProgress(message, current, total);
        return Task.CompletedTask;
    }
}
