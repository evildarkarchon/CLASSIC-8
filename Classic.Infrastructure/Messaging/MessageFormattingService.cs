using Classic.Core.Enums;
using Classic.Core.Interfaces;

namespace Classic.Infrastructure.Messaging;

/// <summary>
/// Default implementation of message formatting service providing consistent styling across handlers
/// </summary>
public class MessageFormattingService : IMessageFormattingService
{
    public string FormatMessage(string message, MessageType type)
    {
        var prefix = GetMessagePrefix(type);
        return $"{prefix} {message}";
    }

    public ConsoleColor GetConsoleColor(MessageType type)
    {
        return type switch
        {
            MessageType.Error => ConsoleColor.Red,
            MessageType.Critical => ConsoleColor.DarkRed,
            MessageType.Warning => ConsoleColor.Yellow,
            MessageType.Success => ConsoleColor.Green,
            MessageType.Debug => ConsoleColor.Gray,
            MessageType.Info => ConsoleColor.White,
            _ => ConsoleColor.White
        };
    }

    public string GetMessagePrefix(MessageType type)
    {
        return type switch
        {
            MessageType.Error => "[ERROR]",
            MessageType.Critical => "[CRITICAL]",
            MessageType.Warning => "[WARNING]",
            MessageType.Success => "[SUCCESS]",
            MessageType.Debug => "[DEBUG]",
            MessageType.Info => "[INFO]",
            _ => "[INFO]"
        };
    }

    public string GetMessageIcon(MessageType type)
    {
        return type switch
        {
            MessageType.Error => "❌",
            MessageType.Critical => "🚨",
            MessageType.Warning => "⚠️",
            MessageType.Success => "✅",
            MessageType.Debug => "🔍",
            MessageType.Info => "ℹ️",
            _ => "ℹ️"
        };
    }
}
