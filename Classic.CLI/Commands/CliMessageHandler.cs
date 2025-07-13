using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.CLI.Commands;

public class CliMessageHandler : IMessageHandler
{
    private readonly bool _showProgress;
    private int _lastProgressLength;

    public CliMessageHandler(bool showProgress = true)
    {
        _showProgress = showProgress;
    }

    public void SendMessage(string message, MessageType type, MessageTarget target)
    {
        if (target == MessageTarget.Gui)
            return; // Skip GUI-only messages in CLI

        switch (type)
        {
            case MessageType.Error:
                Log.Error("{Message}", message);
                break;
            case MessageType.Warning:
                Log.Warning("{Message}", message);
                break;
            case MessageType.Success:
                Log.Information("[SUCCESS] {Message}", message);
                break;
            case MessageType.Debug:
                Log.Debug("{Message}", message);
                break;
            default:
                Log.Information("{Message}", message);
                break;
        }
    }

    public void ReportProgress(string operation, int current, int total)
    {
        if (!_showProgress)
            return;

        var percentage = total > 0 ? current * 100.0 / total : 0;
        var progressBar = CreateProgressBar(percentage);
        var progressText = $"\r{operation}: {progressBar} {percentage:F0}% ({current}/{total})";

        // Clear previous line if it was longer
        if (progressText.Length < _lastProgressLength) Console.Write("\r" + new string(' ', _lastProgressLength));

        Console.Write(progressText);
        _lastProgressLength = progressText.Length;
    }

    public void ClearProgress()
    {
        if (_lastProgressLength > 0)
        {
            Console.Write("\r" + new string(' ', _lastProgressLength) + "\r");
            _lastProgressLength = 0;
        }
    }

    public IDisposable BeginProgressContext(string operation, int total)
    {
        if (_showProgress)
            Log.Information("Starting: {Operation} (Total: {Total})", operation, total);
        return new ProgressContext();
    }

    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        SendMessage(message, MessageType.Info, MessageTarget.Both);
        return Task.CompletedTask;
    }

    public Task SendProgressAsync(int current, int total, string message, CancellationToken cancellationToken = default)
    {
        ReportProgress(message, current, total);
        return Task.CompletedTask;
    }

    private static string CreateProgressBar(double percentage)
    {
        const int barLength = 20;
        var filledLength = (int)(barLength * percentage / 100);
        var bar = new string('█', filledLength) + new string('░', barLength - filledLength);
        return $"[{bar}]";
    }

    private class ProgressContext : IDisposable
    {
        public void Dispose() { }
    }
}
