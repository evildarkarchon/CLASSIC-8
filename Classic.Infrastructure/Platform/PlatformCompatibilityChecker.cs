using Serilog;
using System.Runtime.InteropServices;

namespace Classic.Infrastructure.Platform;

/// <summary>
/// Checks platform compatibility and logs warnings about potential issues
/// </summary>
public static class PlatformCompatibilityChecker
{
    private static readonly ILogger _logger = Log.ForContext(typeof(PlatformCompatibilityChecker));
    
    /// <summary>
    /// Performs a comprehensive platform compatibility check
    /// </summary>
    public static PlatformCompatibilityResult CheckCompatibility()
    {
        var result = new PlatformCompatibilityResult();
        
        _logger.Information("Checking platform compatibility...");
        _logger.Information("Platform Info: {PlatformInfo}", CrossPlatformHelper.GetPlatformInfo());
        
        // Check basic platform support
        result.IsSupported = CheckBasicPlatformSupport();
        
        // Check file system capabilities
        result.FileSystemCapabilities = CheckFileSystemCapabilities();
        
        // Check audio capabilities
        result.AudioCapabilities = CheckAudioCapabilities();
        
        // Check UI capabilities
        result.UICapabilities = CheckUICapabilities();
        
        // Log summary
        LogCompatibilitySummary(result);
        
        return result;
    }
    
    private static bool CheckBasicPlatformSupport()
    {
        try
        {
            var supported = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                          RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                          RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            
            if (!supported)
            {
                _logger.Warning("Platform not explicitly supported: {OS}", RuntimeInformation.OSDescription);
                _logger.Information("Application may still work but some features might be limited");
            }
            
            return supported;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check basic platform support");
            return false;
        }
    }
    
    private static FileSystemCapabilities CheckFileSystemCapabilities()
    {
        var caps = new FileSystemCapabilities();
        
        try
        {
            // Test application data directory
            var appDataDir = CrossPlatformHelper.GetApplicationDataDirectory();
            caps.CanAccessApplicationData = CrossPlatformHelper.TryAccessDirectory(appDataDir);
            
            // Test documents directory
            var documentsDir = CrossPlatformHelper.GetDocumentsDirectory();
            caps.CanAccessDocuments = CrossPlatformHelper.TryAccessDirectory(documentsDir);
            
            // Test temp directory creation
            var tempTestDir = Path.Combine(Path.GetTempPath(), "CLASSIC-8-Test");
            caps.CanCreateDirectories = CrossPlatformHelper.TryCreateDirectory(tempTestDir);
            
            // Clean up test directory
            if (caps.CanCreateDirectories)
            {
                try
                {
                    Directory.Delete(tempTestDir, true);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Failed to clean up test directory");
                }
            }
            
            _logger.Information("File system capabilities: AppData={CanAccessApplicationData}, Documents={CanAccessDocuments}, CreateDir={CanCreateDirectories}", 
                caps.CanAccessApplicationData, caps.CanAccessDocuments, caps.CanCreateDirectories);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check file system capabilities");
        }
        
        return caps;
    }
    
    private static AudioCapabilities CheckAudioCapabilities()
    {
        var caps = new AudioCapabilities();
        
        try
        {
            caps.SupportsConsoleBeep = CrossPlatformHelper.SupportsConsoleBeep();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                caps.HasPulseAudio = CheckCommandExists("paplay");
                caps.HasAlsa = CheckCommandExists("aplay");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                caps.HasCoreAudio = CheckCommandExists("afplay");
                caps.HasOsaScript = CheckCommandExists("osascript");
            }
            
            _logger.Information("Audio capabilities: ConsoleBeep={SupportsConsoleBeep}, Linux(PA={HasPulseAudio}, ALSA={HasAlsa}), macOS(CA={HasCoreAudio}, OSA={HasOsaScript})", 
                caps.SupportsConsoleBeep, caps.HasPulseAudio, caps.HasAlsa, caps.HasCoreAudio, caps.HasOsaScript);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check audio capabilities");
        }
        
        return caps;
    }
    
    private static UICapabilities CheckUICapabilities()
    {
        var caps = new UICapabilities();
        
        try
        {
            // Check for display environment
            caps.HasDisplay = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
                             !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) ||
                             RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                             RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            
            // Check for headless environment
            caps.IsHeadless = Environment.GetEnvironmentVariable("CLASSIC_HEADLESS") == "1" ||
                             Environment.GetEnvironmentVariable("CI") == "true" ||
                             Environment.GetEnvironmentVariable("TERM") == "dumb";
            
            _logger.Information("UI capabilities: HasDisplay={HasDisplay}, IsHeadless={IsHeadless}", 
                caps.HasDisplay, caps.IsHeadless);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check UI capabilities");
        }
        
        return caps;
    }
    
    private static bool CheckCommandExists(string command)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.Start();
            process.WaitForExit(1000); // 1 second timeout
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private static void LogCompatibilitySummary(PlatformCompatibilityResult result)
    {
        var issues = new List<string>();
        
        if (!result.IsSupported)
            issues.Add("Platform not explicitly supported");
        
        if (!result.FileSystemCapabilities.CanAccessApplicationData)
            issues.Add("Cannot access application data directory");
        
        if (!result.FileSystemCapabilities.CanAccessDocuments)
            issues.Add("Cannot access documents directory");
        
        if (!result.FileSystemCapabilities.CanCreateDirectories)
            issues.Add("Cannot create directories");
        
        if (!result.AudioCapabilities.SupportsConsoleBeep && 
            !result.AudioCapabilities.HasPulseAudio && 
            !result.AudioCapabilities.HasAlsa && 
            !result.AudioCapabilities.HasCoreAudio)
            issues.Add("No audio notification support available");
        
        if (!result.UICapabilities.HasDisplay)
            issues.Add("No display environment detected");
        
        if (issues.Any())
        {
            _logger.Warning("Platform compatibility issues detected: {Issues}", string.Join(", ", issues));
            _logger.Information("Application will attempt to use fallbacks for unsupported features");
        }
        else
        {
            _logger.Information("Platform compatibility check passed - all features should work normally");
        }
    }
}

/// <summary>
/// Result of platform compatibility check
/// </summary>
public class PlatformCompatibilityResult
{
    public bool IsSupported { get; set; }
    public FileSystemCapabilities FileSystemCapabilities { get; set; } = new();
    public AudioCapabilities AudioCapabilities { get; set; } = new();
    public UICapabilities UICapabilities { get; set; } = new();
}

/// <summary>
/// File system capabilities
/// </summary>
public class FileSystemCapabilities
{
    public bool CanAccessApplicationData { get; set; }
    public bool CanAccessDocuments { get; set; }
    public bool CanCreateDirectories { get; set; }
}

/// <summary>
/// Audio capabilities
/// </summary>
public class AudioCapabilities
{
    public bool SupportsConsoleBeep { get; set; }
    public bool HasPulseAudio { get; set; }
    public bool HasAlsa { get; set; }
    public bool HasCoreAudio { get; set; }
    public bool HasOsaScript { get; set; }
}

/// <summary>
/// UI capabilities
/// </summary>
public class UICapabilities
{
    public bool HasDisplay { get; set; }
    public bool IsHeadless { get; set; }
}