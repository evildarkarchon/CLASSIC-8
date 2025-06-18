namespace CLASSIC_8.Core.Logging;

/// <summary>
///     Defines the contract for handling messages in both GUI and CLI modes.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    ///     Gets whether the handler is in GUI mode.
    /// </summary>
    bool IsGuiMode { get; }

    /// <summary>
    ///     Displays an informational message.
    /// </summary>
    void Info(string message);

    /// <summary>
    ///     Displays a warning message.
    /// </summary>
    void Warning(string message);

    /// <summary>
    ///     Displays an error message.
    /// </summary>
    void Error(string message);

    /// <summary>
    ///     Displays a debug message.
    /// </summary>
    void Debug(string message);

    /// <summary>
    ///     Displays a status message (typically for progress updates).
    /// </summary>
    void Status(string message);

    /// <summary>
    ///     Displays a special notice message.
    /// </summary>
    void Notice(string message);

    /// <summary>
    ///     Displays a completion message.
    /// </summary>
    void Complete(string message);

    /// <summary>
    ///     Shows a yes/no dialog and returns the user's choice.
    /// </summary>
    /// <param name="message">The question to ask.</param>
    /// <param name="title">The dialog title (for GUI mode).</param>
    /// <returns>True if the user selected yes, false otherwise.</returns>
    bool ShowYesNoDialog(string message, string title = "CLASSIC");
}