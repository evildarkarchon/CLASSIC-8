using Classic.Core.Enums;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for formatting messages consistently across different message handlers
/// </summary>
public interface IMessageFormattingService
{
    /// <summary>
    /// Formats a message with the appropriate styling for the given message type
    /// </summary>
    /// <param name="message">The message content</param>
    /// <param name="type">The type of message</param>
    /// <returns>Formatted message string</returns>
    string FormatMessage(string message, MessageType type);

    /// <summary>
    /// Gets the console color associated with a message type
    /// </summary>
    /// <param name="type">The message type</param>
    /// <returns>Console color for the message type</returns>
    ConsoleColor GetConsoleColor(MessageType type);

    /// <summary>
    /// Gets a standardized prefix for a message type
    /// </summary>
    /// <param name="type">The message type</param>
    /// <returns>Prefix string for the message type</returns>
    string GetMessagePrefix(MessageType type);

    /// <summary>
    /// Gets an icon or symbol representing the message type for GUI display
    /// </summary>
    /// <param name="type">The message type</param>
    /// <returns>Icon string for GUI display</returns>
    string GetMessageIcon(MessageType type);
}
