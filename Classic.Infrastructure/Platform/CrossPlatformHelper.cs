using System.Runtime.InteropServices;
using Serilog;

namespace Classic.Infrastructure.Platform;

/// <summary>
/// Helper class for cross-platform compatibility
/// </summary>
public static class CrossPlatformHelper
{
    private static readonly ILogger _logger = Log.ForContext(typeof(CrossPlatformHelper));
    
    /// <summary>
    /// Gets the platform-specific application data directory
    /// </summary>
    public static string GetApplicationDataDirectory()
    {
        try
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get LocalApplicationData folder, using fallback");
            
            // Fallback for unsupported platforms
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(homeDir))
            {
                homeDir = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
            }
            
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? Path.Combine(homeDir, "AppData", "Local")
                : Path.Combine(homeDir, ".local", "share");
        }
    }
    
    /// <summary>
    /// Gets the platform-specific documents directory
    /// </summary>
    public static string GetDocumentsDirectory()
    {
        try
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get MyDocuments folder, using fallback");
            
            // Fallback for unsupported platforms
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(homeDir))
            {
                homeDir = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
            }
            
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? Path.Combine(homeDir, "Documents")
                : Path.Combine(homeDir, "Documents");
        }
    }
    
    /// <summary>
    /// Safely attempts to create a directory with cross-platform error handling
    /// </summary>
    public static bool TryCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.Debug("Created directory: {Path}", path);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to create directory: {Path}", path);
            return false;
        }
    }
    
    /// <summary>
    /// Safely attempts to access a directory with cross-platform error handling
    /// </summary>
    public static bool TryAccessDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return false;
                
            // Try to enumerate directories to test access
            Directory.EnumerateDirectories(path).Take(1).Any();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Warning("Access denied to directory: {Path}", path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to access directory: {Path}", path);
            return false;
        }
    }
    
    /// <summary>
    /// Gets the platform-specific line ending
    /// </summary>
    public static string GetLineEnding()
    {
        return Environment.NewLine;
    }
    
    /// <summary>
    /// Gets the platform-specific path separator
    /// </summary>
    public static char GetPathSeparator()
    {
        return Path.DirectorySeparatorChar;
    }
    
    /// <summary>
    /// Converts a path to use platform-specific separators
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
            
        return path.Replace('\\', Path.DirectorySeparatorChar)
                   .Replace('/', Path.DirectorySeparatorChar);
    }
    
    /// <summary>
    /// Safely combines paths with error handling
    /// </summary>
    public static string SafePathCombine(params string[] paths)
    {
        try
        {
            return Path.Combine(paths);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to combine paths: {Paths}", string.Join(", ", paths));
            // Fallback: join with platform separator
            return string.Join(Path.DirectorySeparatorChar.ToString(), paths);
        }
    }
    
    /// <summary>
    /// Checks if the current platform supports console beep
    /// </summary>
    public static bool SupportsConsoleBeep()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
    
    /// <summary>
    /// Safely attempts to beep the console
    /// </summary>
    public static void TryConsoleBeep()
    {
        if (!SupportsConsoleBeep())
        {
            _logger.Debug("Console beep not supported on this platform");
            return;
        }
        
        try
        {
            Console.Beep();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to beep console");
        }
    }
    
    /// <summary>
    /// Gets platform-specific information for debugging
    /// </summary>
    public static string GetPlatformInfo()
    {
        return $"OS: {RuntimeInformation.OSDescription}, " +
               $"Architecture: {RuntimeInformation.OSArchitecture}, " +
               $"Framework: {RuntimeInformation.FrameworkDescription}, " +
               $"Process Architecture: {RuntimeInformation.ProcessArchitecture}";
    }
}