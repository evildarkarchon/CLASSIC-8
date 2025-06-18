namespace CLASSIC_8.Core.Logging;

/// <summary>
///     Provides extension methods for easier access to the message handler.
/// </summary>
public static class MessageHandlerExtensions
{
    private static IMessageHandler? _messageHandler;

    /// <summary>
    ///     Gets the global message handler instance.
    /// </summary>
    public static IMessageHandler MessageHandler
    {
        get
        {
            if (_messageHandler == null)
                throw new InvalidOperationException(
                    "MessageHandler has not been initialized. Call InitializeMessageHandler() first.");
            return _messageHandler;
        }
    }

    /// <summary>
    ///     Initializes the global message handler instance.
    /// </summary>
    public static void InitializeMessageHandler(IMessageHandler messageHandler)
    {
        _messageHandler = messageHandler;
    }

    /// <summary>
    ///     Displays an informational message.
    /// </summary>
    public static void MsgInfo(string message)
    {
        MessageHandler.Info(message);
    }

    /// <summary>
    ///     Displays a warning message.
    /// </summary>
    public static void MsgWarning(string message)
    {
        MessageHandler.Warning(message);
    }

    /// <summary>
    ///     Displays an error message.
    /// </summary>
    public static void MsgError(string message)
    {
        MessageHandler.Error(message);
    }

    /// <summary>
    ///     Displays a debug message.
    /// </summary>
    public static void MsgDebug(string message)
    {
        MessageHandler.Debug(message);
    }

    /// <summary>
    ///     Displays a status message.
    /// </summary>
    public static void MsgStatus(string message)
    {
        MessageHandler.Status(message);
    }

    /// <summary>
    ///     Displays a special notice message.
    /// </summary>
    public static void MsgNotice(string message)
    {
        MessageHandler.Notice(message);
    }

    /// <summary>
    ///     Displays a completion message.
    /// </summary>
    public static void MsgComplete(string message)
    {
        MessageHandler.Complete(message);
    }

    /// <summary>
    ///     Shows a yes/no dialog and returns the user's choice.
    /// </summary>
    public static bool ShowYesNo(string message, string title = "CLASSIC")
    {
        return MessageHandler.ShowYesNoDialog(message, title);
    }
}