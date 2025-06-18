# Logging System Implementation

This directory contains the C# port of the Python logging system from CLASSIC.

## Key Components

### LoggingConfiguration Class
Central configuration for NLog logging:
- **File Target**: `CLASSIC Journal.log` in application directory
- **Log Format**: `{timestamp} | {level} | {message}{exception}`
- **Log Retention**: Automatic deletion after 7 days
- **Log Rotation**: Archives after 10MB, keeps 3 archive files
- **Concurrent Writes**: Supports multiple threads writing simultaneously

### IMessageHandler Interface
Defines the contract for unified message handling across GUI and CLI modes:
- `Info()`, `Warning()`, `Error()`, `Debug()` - Standard logging levels
- `Status()`, `Notice()`, `Complete()` - Special message types
- `ShowYesNoDialog()` - Interactive user prompts
- `IsGuiMode` - Determines display behavior

### MessageHandler Class
Implementation of IMessageHandler that:
- **Dual Output**: Logs to file AND displays to console/GUI
- **Emoji Stripping**: Removes emoji characters from log entries to prevent encoding issues
- **Color-coded Console**: Different colors for different message types in CLI mode
- **GUI Ready**: Placeholder methods for future GUI integration

### MessageHandlerExtensions Static Class
Provides global access to message handling:
- `MsgInfo()`, `MsgWarning()`, `MsgError()` - Convenient global methods
- **Singleton Pattern**: Maintains global message handler instance
- **Initialization Check**: Throws exception if not properly initialized

## Usage Examples

### Basic Setup (Application Startup)
```csharp
// Configure logging first
LoggingConfiguration.Configure();

// Initialize message handler
var messageHandler = new MessageHandler(parent: null, isGuiMode: false);
MessageHandlerExtensions.InitializeMessageHandler(messageHandler);

// Application can now use logging
```

### Using Direct Logger
```csharp
var logger = LogManager.GetLogger("CLASSIC.MyComponent");
logger.Info("Component initialized");
logger.Error("Something went wrong");
```

### Using Message Handler
```csharp
var handler = new MessageHandler();
handler.Info("Processing file...");
handler.Warning("File already exists");
handler.Error("Unable to read file");
handler.Complete("Process finished successfully");
```

### Using Global Extensions
```csharp
MessageHandlerExtensions.MsgInfo("Application starting...");
MessageHandlerExtensions.MsgStatus("Loading configuration...");
MessageHandlerExtensions.MsgComplete("Ready!");

// Interactive dialog
bool result = MessageHandlerExtensions.ShowYesNo("Continue processing?", "Confirm");
```

## Key Features Ported from Python

1. **Log File Management**: 
   - Automatic creation of `CLASSIC Journal.log`
   - 7-day retention policy with automatic cleanup
   - Rotation to prevent oversized files

2. **Dual Mode Operation**:
   - CLI mode with colored console output
   - GUI mode preparation for future Avalonia integration

3. **Emoji Handling**:
   - Strips emoji characters from log entries
   - Prevents encoding issues on different systems

4. **Hierarchical Loggers**:
   - Uses namespaced loggers (e.g., "CLASSIC.MessageHandler")
   - Consistent with Python logging structure

5. **Message Types**:
   - Standard: Info, Warning, Error, Debug
   - Special: Status, Notice, Complete
   - Interactive: Yes/No dialogs

## Configuration Files

### NLog.config
XML configuration file that defines:
- File target with rotation settings
- Console target (optional for development)
- Logger rules and levels
- Layout formatting

## Integration with YAML System

The logging system integrates with the YAML configuration:
```csharp
// YAML cache uses logging
var yamlCache = new YamlSettingsCache(gameManager);
// Logger automatically configured via NLog.config

// Message handler can be used throughout application
MessageHandlerExtensions.MsgInfo("YAML configuration loaded");
```

## Differences from Python Implementation

1. **Configuration**: Uses NLog.config instead of programmatic setup
2. **Thread Safety**: Built-in thread safety with ConcurrentWrites
3. **Type Safety**: Strongly-typed interfaces and classes
4. **DI Ready**: Designed for dependency injection patterns
5. **Performance**: Compiled regex for emoji detection