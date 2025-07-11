# Audio Notification System

## Overview

CLASSIC-8 includes a comprehensive cross-platform audio notification system that plays custom embedded sound files for different notification types. The system is designed to work reliably across Windows, Linux, and macOS while providing appropriate fallbacks when audio is unavailable.

## Sound Files

### Embedded Resources
The application includes two custom sound files embedded as resources:

- **classic_notify.wav** (276KB): Used for success, information, and warning notifications
- **classic_error.wav** (154KB): Used for error notifications

### Audio Specifications
- **Format**: WAV (uncompressed) for maximum compatibility
- **Volume**: All playback is automatically set to 50% volume as requested
- **Location**: `Classic.Avalonia/Resources/Audio/`
- **Embedding**: Files are embedded as resources in the Avalonia project

## Architecture

### Core Components

#### IAudioService Interface
```csharp
public interface IAudioService
{
    Task PlayEmbeddedResourceAsync(string resourceName, double volume = 0.5);
    Task PlayNotificationAsync(NotificationType type, double volume = 0.5);
    bool IsAudioSupported();
}
```

#### AudioService Implementation
- **Location**: `Classic.Infrastructure.Services.AudioService`
- **Features**:
  - Automatic resource extraction to temporary files
  - Platform-specific audio playback
  - Volume control (0.0 to 1.0)
  - Comprehensive fallback mechanisms
  - Cleanup of temporary files

#### NotificationService Integration
- **Location**: `Classic.Infrastructure.Services.NotificationService`
- **Integration**: Automatically plays appropriate sounds based on notification type
- **Settings**: Respects the `SoundOnCompletion` setting

## Platform Support

### Windows
- **Primary**: PowerShell MediaPlayer with volume control
- **Features**:
  - Full .WAV file support
  - Precise volume control
  - Async playback
- **Fallback**: Silent operation with logging

### Linux
- **Audio Systems Supported**:
  1. **PulseAudio** (`paplay`) - Primary choice
  2. **ALSA** (`aplay`) - Fallback for systems without PulseAudio
  3. **FFmpeg** (`ffplay`) - Advanced audio support
  4. **MPV** (`mpv`) - Multimedia player fallback
- **Features**:
  - Volume control where supported
  - Automatic system detection
  - Graceful degradation
- **Fallback**: Silent operation with logging

### macOS
- **Primary**: `afplay` with volume control
- **Features**:
  - Native .WAV support
  - Volume control (`-v` flag)
  - Async playback
- **Fallback**: Silent operation with logging

## Usage

### Direct Audio Service Usage
```csharp
// Inject the service
private readonly IAudioService _audioService;

// Play notification sounds
await _audioService.PlayNotificationAsync(NotificationType.Success, volume: 0.5);
await _audioService.PlayNotificationAsync(NotificationType.Error, volume: 0.5);

// Play specific embedded resources
await _audioService.PlayEmbeddedResourceAsync("classic_notify.wav", volume: 0.5);
await _audioService.PlayEmbeddedResourceAsync("classic_error.wav", volume: 0.5);

// Check if audio is supported
if (_audioService.IsAudioSupported())
{
    // Audio features are available
}
```

### Notification Service Integration
```csharp
// Audio automatically plays when notifications are shown
await notificationService.ShowToastAsync("Success", "Operation completed", NotificationType.Success);
await notificationService.ShowToastAsync("Error", "Operation failed", NotificationType.Error);
```

### Settings Integration
```csharp
// Audio respects user settings
var settings = settingsService.Settings;
if (settings.SoundOnCompletion)
{
    // Audio will play for notifications
}
```

## Configuration

### Dependency Injection
```csharp
// In ServiceCollectionExtensions.cs
services.AddSingleton<IAudioService, Services.AudioService>();
services.AddSingleton<INotificationService, Services.NotificationService>();
```

### Project Configuration
```xml
<!-- In Classic.Avalonia.csproj -->
<ItemGroup>
    <EmbeddedResource Include="Resources\Audio\**" />
</ItemGroup>
```

## Sound Mapping

| Notification Type | Sound File | Description |
|-------------------|------------|-------------|
| Success | classic_notify.wav | Positive completion sound |
| Information | classic_notify.wav | General information sound |
| Warning | classic_notify.wav | Warning notification sound |
| Error | classic_error.wav | Error notification sound |

## Troubleshooting

### Linux Audio Issues
```bash
# Install required dependencies
sudo apt-get install pulseaudio-utils alsa-utils ffmpeg mpv  # Ubuntu/Debian
sudo dnf install pulseaudio-utils alsa-utils ffmpeg mpv      # Fedora

# Test audio manually
paplay /path/to/classic_notify.wav
aplay /path/to/classic_notify.wav
ffplay -nodisp -autoexit /path/to/classic_notify.wav
```

### macOS Permission Issues
- Grant file system access in System Preferences > Security & Privacy
- Ensure `afplay` is available (standard on macOS)

### Windows PowerShell Issues
- Ensure PowerShell is available and execution policy allows scripts
- The system falls back to silent operation if PowerShell is unavailable

### General Debugging
```bash
# Check platform audio support
dotnet run --project Classic.Avalonia --verbosity=debug

# Check embedded resources
dotnet build --verbosity=detailed
```

## Performance Considerations

### Resource Management
- **Temporary Files**: Audio files are extracted to temporary directory on first use
- **Cleanup**: Temporary files are automatically cleaned up on service disposal
- **Caching**: Extracted files are cached for subsequent use
- **Memory**: Minimal memory footprint due to streaming extraction

### Async Operations
- All audio playback is asynchronous
- Non-blocking UI operations
- Proper cancellation support
- Timeout handling for hung processes

## Security Considerations

### Embedded Resources
- Sound files are embedded in the application binary
- No external file dependencies
- Reduced attack surface compared to external files

### Process Execution
- Audio commands are executed with minimal privileges
- No shell injection vulnerabilities
- Proper argument escaping and validation

## Future Enhancements

### Planned Features
- Additional notification sounds for different event types
- User-customizable sound files
- Audio format support beyond WAV
- Volume control in user settings
- Audio device selection

### Performance Optimizations
- Native audio API integration
- Reduced temporary file usage
- Improved platform detection
- Better error handling and retry logic

## Conclusion

The CLASSIC-8 audio notification system provides a robust, cross-platform solution for audio feedback that enhances the user experience while maintaining reliability across different operating systems. The embedded resource approach ensures consistency and reduces dependencies while providing appropriate fallbacks for environments where audio is not available.