# Cross-Platform Compatibility Guide

## Overview

CLASSIC-8 has been designed with cross-platform compatibility in mind, supporting Windows, Linux, and macOS. This document outlines the specific measures taken to ensure the application works reliably across different operating systems.

## Platform Support

### ✅ Fully Supported Platforms
- **Windows 10/11** (x64, ARM64)
- **Linux** (x64, ARM64)
  - Ubuntu 20.04+ 
  - Debian 11+
  - Fedora 35+
  - Arch Linux
  - openSUSE Leap 15.3+
- **macOS** (x64, ARM64)
  - macOS 11 (Big Sur)+
  - macOS 12 (Monterey)+  
  - macOS 13 (Ventura)+
  - macOS 14 (Sonoma)+

### ⚠️ Limited Support Platforms
- **FreeBSD** - Basic functionality, limited audio support
- **Other Unix-like systems** - May work with reduced functionality

## Cross-Platform Features

### 1. File System Operations
- **Path Handling**: Uses `Path.Combine()` and `Path.DirectorySeparatorChar` for cross-platform path construction
- **Special Folders**: Platform-appropriate application data and documents directories
- **Fallback Directories**: Automatic fallback to `~/.local/share` on Linux/macOS when standard directories aren't available

### 2. Audio Notifications
- **Windows**: Uses `rundll32` for system sounds with `Console.Beep()` fallback
- **Linux**: Supports multiple audio systems:
  - PulseAudio (`paplay`)
  - ALSA (`aplay`)
  - Speaker test (`speaker-test`)
  - Bell character (`echo`)
- **macOS**: Uses `afplay` for system sounds with `osascript` fallback
- **Fallback**: Graceful degradation when audio isn't available

### 3. UI Framework
- **Avalonia UI**: Cross-platform desktop framework
- **ReactiveUI**: Cross-platform MVVM framework
- **File Dialogs**: Platform-native file/folder selection
- **Styling**: Consistent dark theme across platforms

### 4. Threading and Async
- **Avalonia Dispatcher**: Cross-platform UI thread management
- **Task-based Async**: Standard .NET async/await patterns
- **Thread-safe Operations**: Proper synchronization for all platforms

## Cross-Platform Helpers

### CrossPlatformHelper Class
Located in `Classic.Infrastructure.Platform.CrossPlatformHelper`, provides:

```csharp
// Platform-specific directory access
string appDataDir = CrossPlatformHelper.GetApplicationDataDirectory();
string documentsDir = CrossPlatformHelper.GetDocumentsDirectory();

// Safe file operations
bool success = CrossPlatformHelper.TryCreateDirectory(path);
bool canAccess = CrossPlatformHelper.TryAccessDirectory(path);

// Path normalization
string normalizedPath = CrossPlatformHelper.NormalizePath(inputPath);
string combinedPath = CrossPlatformHelper.SafePathCombine(parts);

// Platform detection
bool supportsBeep = CrossPlatformHelper.SupportsConsoleBeep();
string platformInfo = CrossPlatformHelper.GetPlatformInfo();
```

### PlatformCompatibilityChecker
Runs automatic compatibility checks on startup:

- **Basic Platform Support**: Detects Windows/Linux/macOS
- **File System Capabilities**: Tests directory access and creation
- **Audio Capabilities**: Checks available audio systems
- **UI Capabilities**: Detects display environment and headless mode

## Fallback Mechanisms

### 1. Directory Access Fallbacks
```
Primary: Environment.SpecialFolder.LocalApplicationData
Linux/macOS Fallback: ~/.local/share
Emergency Fallback: /tmp or %TEMP%
```

### 2. Audio Notification Fallbacks
```
Windows: rundll32 → Console.Beep() → Silent
Linux: paplay → aplay → speaker-test → echo → Silent  
macOS: afplay → osascript → Silent
Other: Silent (with logging)
```

### 3. File Operation Fallbacks
```
Primary: Standard .NET file operations
Fallback: Try-catch with detailed logging
Emergency: Graceful degradation with user notification
```

## Environment Variables

### Configuration
- `CLASSIC_HEADLESS=1` - Disables UI features for server environments
- `HOME` - Used for directory fallbacks on Unix systems
- `DISPLAY` / `WAYLAND_DISPLAY` - UI capability detection on Linux

### CI/CD Detection
- `CI=true` - Continuous integration environment detection
- `TERM=dumb` - Terminal capability detection

## Testing Across Platforms

### Automated Testing
- **GitHub Actions**: Windows, Linux, macOS builds
- **Unit Tests**: Cross-platform compatibility tests
- **Integration Tests**: Platform-specific feature validation

### Manual Testing Recommendations
1. **File Operations**: Test folder selection on each platform
2. **Audio**: Verify notification sounds work or gracefully fail
3. **Paths**: Ensure proper path handling with special characters
4. **Permissions**: Test with restricted directory access

## Troubleshooting

### Common Issues

#### Linux Audio Issues
```bash
# Install audio dependencies
sudo apt-get install pulseaudio-utils alsa-utils  # Ubuntu/Debian
sudo dnf install pulseaudio-utils alsa-utils      # Fedora
```

#### macOS Permission Issues
```bash
# Grant file system access in System Preferences > Security & Privacy
# Or run with elevated permissions for testing
```

#### General Debugging
```bash
# Check platform compatibility
dotnet run --project Classic.Avalonia --verbosity=debug

# Environment information
echo $DISPLAY $WAYLAND_DISPLAY $HOME $CLASSIC_HEADLESS
```

### Log Analysis
Platform compatibility issues are logged with these patterns:
- `Platform compatibility issues detected: {Issues}`
- `Failed to play {Platform} system sound`
- `Cannot access {FolderType} folder: {Path}`

## Best Practices

### For Developers
1. **Always use Path.Combine()** for file paths
2. **Test on multiple platforms** during development
3. **Use CrossPlatformHelper** for platform-specific operations
4. **Implement fallbacks** for platform-specific features
5. **Log platform compatibility** issues for debugging

### For Users
1. **Check logs** for platform compatibility warnings
2. **Install audio dependencies** on Linux if needed
3. **Grant file permissions** on macOS if requested
4. **Use absolute paths** for custom directories
5. **Report platform-specific issues** with platform details

## Future Enhancements

### Planned Improvements
- **Android/iOS Support**: Avalonia mobile support when available
- **ARM Support**: Additional ARM architecture testing
- **Package Managers**: Native package formats (MSI, DEB, RPM, DMG)
- **Container Support**: Docker and Flatpak packaging

### Performance Optimizations
- **Platform-specific optimizations**: Audio/file system tuning
- **Memory management**: Platform-specific garbage collection
- **Threading**: Platform-optimal thread pool configuration

## Conclusion

CLASSIC-8 prioritizes cross-platform compatibility while maintaining full functionality on each supported platform. The application gracefully handles platform differences and provides appropriate fallbacks when features are unavailable.

For platform-specific issues or feature requests, please create an issue in the GitHub repository with detailed platform information from the application logs.