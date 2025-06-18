using System.Text.RegularExpressions;
using NLog;

namespace CLASSIC_8.Core.Logging;

/// <summary>
/// Handles message display and logging for both GUI and CLI modes.
/// </summary>
public class MessageHandler : IMessageHandler
{
    private static readonly Logger Logger = LogManager.GetLogger("CLASSIC.MessageHandler");
    private static readonly Regex EmojiRegex = new(@"[\uD83D\uDE00-\uD83D\uDE4F]|[\uD83C\uDF00-\uD83D\uDDFF]|[\uD83D\uDE80-\uD83D\uDEFF]|[\u2600-\u26FF]|[\u2700-\u27BF]", RegexOptions.Compiled);
    
    private readonly object? _parent;
    
    public bool IsGuiMode { get; }
    
    public MessageHandler(object? parent = null, bool isGuiMode = false)
    {
        _parent = parent;
        IsGuiMode = isGuiMode;
    }
    
    public void Info(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Info(cleanMessage);
        
        if (IsGuiMode)
        {
            // TODO: Display in GUI
        }
        else
        {
            Console.WriteLine($"[INFO] {message}");
        }
    }
    
    public void Warning(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Warn(cleanMessage);
        
        if (IsGuiMode)
        {
            // TODO: Display in GUI
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] {message}");
            Console.ResetColor();
        }
    }
    
    public void Error(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Error(cleanMessage);
        
        if (IsGuiMode)
        {
            // TODO: Display in GUI
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }
    }
    
    public void Debug(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Debug(cleanMessage);
        
        if (IsGuiMode)
        {
            // TODO: Display in GUI debug console if available
        }
        else if (Logger.IsDebugEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[DEBUG] {message}");
            Console.ResetColor();
        }
    }
    
    public void Status(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Info($"[STATUS] {cleanMessage}");
        
        if (IsGuiMode)
        {
            // TODO: Update status bar in GUI
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[STATUS] {message}");
            Console.ResetColor();
        }
    }
    
    public void Notice(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Info($"[NOTICE] {cleanMessage}");
        
        if (IsGuiMode)
        {
            // TODO: Display special notice in GUI
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[NOTICE] {message}");
            Console.ResetColor();
        }
    }
    
    public void Complete(string message)
    {
        var cleanMessage = StripEmojis(message);
        Logger.Info($"[COMPLETE] {cleanMessage}");
        
        if (IsGuiMode)
        {
            // TODO: Display completion message in GUI
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[COMPLETE] {message}");
            Console.ResetColor();
        }
    }
    
    public bool ShowYesNoDialog(string message, string title = "CLASSIC")
    {
        var cleanMessage = StripEmojis(message);
        
        if (IsGuiMode)
        {
            // TODO: Show GUI dialog
            Logger.Info($"[DIALOG] {title}: {cleanMessage} - User response: Pending");
            return false; // Placeholder
        }
        else
        {
            Console.WriteLine($"\n{title}: {message}");
            Console.Write("(Y/N): ");
            
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)
                {
                    Console.WriteLine("Y");
                    Logger.Info($"[DIALOG] {title}: {cleanMessage} - User response: Yes");
                    return true;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    Console.WriteLine("N");
                    Logger.Info($"[DIALOG] {title}: {cleanMessage} - User response: No");
                    return false;
                }
            }
        }
    }
    
    private static string StripEmojis(string input)
    {
        // Remove emoji characters to avoid encoding issues in log files
        return EmojiRegex.Replace(input, string.Empty);
    }
}