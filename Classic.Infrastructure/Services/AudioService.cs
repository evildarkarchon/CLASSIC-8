using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Infrastructure.Platform;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Cross-platform audio service for playing embedded notification sounds
/// </summary>
public class AudioService : IAudioService
{
    private readonly ILogger _logger;
    private readonly string _tempDirectory;
    private readonly Dictionary<string, string> _extractedFiles = new();

    public AudioService(ILogger logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Classic-Audio");
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task PlayEmbeddedResourceAsync(string resourceName, double volume = 0.5)
    {
        try
        {
            // Clamp volume between 0.0 and 1.0
            volume = Math.Clamp(volume, 0.0, 1.0);

            var filePath = await ExtractEmbeddedResourceAsync(resourceName);
            if (filePath == null)
            {
                _logger.Warning("Failed to extract embedded resource: {ResourceName}", resourceName);
                return;
            }

            await PlayAudioFileAsync(filePath, volume);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to play embedded resource: {ResourceName}", resourceName);
        }
    }

    public async Task PlayNotificationAsync(NotificationType type, double volume = 0.5)
    {
        var resourceName = type switch
        {
            NotificationType.Success => "classic_notify.wav",
            NotificationType.Information => "classic_notify.wav",
            NotificationType.Warning => "classic_notify.wav",
            NotificationType.Error => "classic_error.wav",
            _ => "classic_notify.wav"
        };

        await PlayEmbeddedResourceAsync(resourceName, volume);
    }

    public bool IsAudioSupported()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return CheckWindowsAudioSupport();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return CheckLinuxAudioSupport();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return CheckMacAudioSupport();

        return false;
    }

    private async Task<string?> ExtractEmbeddedResourceAsync(string resourceName)
    {
        if (_extractedFiles.TryGetValue(resourceName, out var existingPath) && File.Exists(existingPath))
            return existingPath;

        try
        {
            // Get the Avalonia assembly that contains the embedded resources
            var avaloniaAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Classic.Avalonia");

            if (avaloniaAssembly == null)
            {
                _logger.Warning("Classic.Avalonia assembly not found");
                return null;
            }

            var resourcePath = $"Classic.Avalonia.Resources.Audio.{resourceName}";

            using var stream = avaloniaAssembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                _logger.Warning("Embedded resource not found: {ResourcePath}", resourcePath);
                return null;
            }

            var filePath = Path.Combine(_tempDirectory, resourceName);
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            _extractedFiles[resourceName] = filePath;
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract embedded resource: {ResourceName}", resourceName);
            return null;
        }
    }

    private async Task PlayAudioFileAsync(string filePath, double volume)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await PlayWindowsAudioAsync(filePath, volume);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await PlayLinuxAudioAsync(filePath, volume);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            await PlayMacAudioAsync(filePath, volume);
        else
            _logger.Information("Audio playback not supported on this platform");
    }

    private async Task PlayWindowsAudioAsync(string filePath, double volume)
    {
        try
        {
            // Use PowerShell to play audio with volume control
            var volumePercent = (int)(volume * 100);
            var script = $@"
                Add-Type -AssemblyName presentationCore
                $mediaPlayer = New-Object system.windows.media.mediaplayer
                $mediaPlayer.Volume = {volume:F2}
                $mediaPlayer.Open('{filePath}')
                $mediaPlayer.Play()
                Start-Sleep -Seconds 3
                $mediaPlayer.Stop()
                $mediaPlayer.Close()
            ";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                _logger.Warning("Windows audio playback failed with exit code: {ExitCode}", process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to play Windows audio");
        }
    }

    private async Task PlayLinuxAudioAsync(string filePath, double volume)
    {
        var audioCommands = new[]
        {
            ("paplay", $"--volume={volume:F2} \"{filePath}\""),
            ("aplay", $"-q \"{filePath}\""), // aplay doesn't support volume directly
            ("ffplay", $"-nodisp -autoexit -volume {(int)(volume * 100)} \"{filePath}\""),
            ("mpv", $"--no-video --volume={volume * 100:F0} \"{filePath}\"")
        };

        foreach (var (command, args) in audioCommands)
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.Debug("Successfully played audio using {Command}", command);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to play audio using {Command}", command);
                continue;
            }

        _logger.Warning("All Linux audio playback methods failed");
    }

    private async Task PlayMacAudioAsync(string filePath, double volume)
    {
        try
        {
            // Use afplay with volume control
            var volumeArg = $"-v {volume:F2}";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "afplay",
                    Arguments = $"{volumeArg} \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                _logger.Warning("macOS audio playback failed with exit code: {ExitCode}", process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to play macOS audio");
        }
    }

    private bool CheckWindowsAudioSupport()
    {
        try
        {
            return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                       "powershell.exe")) ||
                   File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
                       "WindowsPowerShell", "v1.0", "powershell.exe"));
        }
        catch
        {
            return false;
        }
    }

    private bool CheckLinuxAudioSupport()
    {
        var audioCommands = new[] { "paplay", "aplay", "ffplay", "mpv" };

        foreach (var command in audioCommands)
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = command,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0) return true;
            }
            catch
            {
                continue;
            }

        return false;
    }

    private bool CheckMacAudioSupport()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "afplay",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, true);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to cleanup temporary audio files");
        }
    }
}
