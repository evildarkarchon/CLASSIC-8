# GitHub Copilot Instructions for CLASSIC-8

## Project Overview

CLASSIC-8 is a C# port of a Python crash log analysis tool for Bethesda games (Fallout 4, Skyrim SE/VR). The application follows clean architecture with domain-driven design, analyzing game crash logs to identify mod conflicts and plugin issues.

## Architecture & Project Structure

### Clean Architecture Layers
- **`Classic.Core/`** - Domain models, interfaces, enums, exceptions
- **`Classic.Infrastructure/`** - YAML config, file I/O, logging, messaging, DI
- **`Classic.ScanLog/`** - Crash log parsing and analysis orchestration  
- **`Classic.ScanGame/`** - Game file scanning (XSE, Reshade, ENB management)
- **`Classic.Avalonia/`** - Cross-platform desktop GUI using Avalonia + ReactiveUI
- **`Classic.CLI/`** - Command-line interface
- **`tests/`** - xUnit test projects with FluentAssertions and Moq
- **`Code to Port/`** - Contains reference code from the original Python implementation for porting

### Key Design Patterns
- **MVVM with ReactiveUI** - All ViewModels inherit from `ReactiveObject`, use `ReactiveCommand` for async operations
- **Dependency Injection** - Microsoft.Extensions.DI throughout, register services in `ServiceCollectionExtensions.cs`
- **Producer-Consumer** - Use `System.Threading.Channels` for async file processing pipelines
- **Message Handling** - Abstract `IMessageHandler` supporting both CLI (`ConsoleMessageHandler`) and GUI (`GuiMessageHandler`)
- **Settings Management** - YAML-based with `YamlDotNet`, cached via `IYamlSettings` interface

## Essential Development Workflows

### Building & Running
```powershell
# Build solution
dotnet build

# Run GUI application  
dotnet run --project Classic.Avalonia

# Run tests
dotnet test

# Build specific projects
dotnet build Classic.Core/
dotnet build Classic.Infrastructure/
```

### Service Registration Pattern
```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddClassicInfrastructure(this IServiceCollection services)
{
    services.AddSingleton<IYamlSettings, YamlSettings>();
    services.AddScoped<ISettingsService, SettingsService>();
    services.AddSingleton<Serilog.ILogger>(Log.Logger);
    return services;
}
```

## Project-Specific Conventions

### Logging Standard
- **Serilog only** - Never use `Microsoft.Extensions.Logging`, always inject `Serilog.ILogger`
- Structured logging: `_logger.Information("Processing {LogFile} with {PluginCount} plugins", path, count)`

### Async Patterns
- Always use `ConfigureAwait(false)` in library code
- Pass `CancellationToken` through async call chains
- Use `ValueTask<T>` for hot paths, `Task<T>` elsewhere

### UI Integration
- Toast notifications: `NotificationService` → `ToastContainer` in MainWindow
- ViewModels get services via constructor injection from `Program.ConfigureServices()`
- ReactiveUI commands: `ReactiveCommand.CreateFromTask()` with `WhenAnyValue()` for state management

### File System Abstraction
- Use `System.IO.Abstractions` interfaces (`IFileSystem`) for testability
- Mock with `TestingHelpers` in tests

## Critical Integration Points

### Scan Orchestration Flow
`IScanOrchestrator` → `ComprehensiveScanOrchestrator` coordinates:
1. File validation & parsing via `ICrashLogParser`
2. Analysis through `IPluginAnalyzer` and `IFormIdAnalyzer` 
3. Progress reporting via `IMessageHandler.ReportProgress()`
4. Result aggregation in `ScanResult` models

### Settings Architecture
- `ISettingsService` provides strongly-typed access to `ClassicSettings`
- YAML files cached by `YamlSettings` with file watching for hot reload
- UI binds directly to ViewModel properties that auto-persist

### Cross-Component Communication
- GUI notifications: `INotificationService.NotificationAdded` event → `ToastContainer.ShowToastAsync()`
- Progress updates: `IMessageHandler` → `IProgressService` → ViewModel property updates
- Game file management: `IGameFileManager` handles backups/restore for XSE, ENB, Reshade, Vulkan

## Testing Guidelines

- Domain logic in `Classic.Core.Tests/` - pure unit tests
- Infrastructure in `Classic.Infrastructure.Tests/` - use mocks for file system
- Use `FluentAssertions` for readable assertions: `result.Should().NotBeNull()`
- Mock pattern: `Mock<IFileSystem>()` with `TestingHelpers` for file operations

## Key Files for Understanding Context

- `Classic.Avalonia/ViewModels/MainWindowViewModel.cs` - Primary UI orchestration
- `Classic.Infrastructure/Extensions/ServiceCollectionExtensions.cs` - DI registration  
- `Classic.ScanLog/Orchestration/ComprehensiveScanOrchestrator.cs` - Core processing logic
- `Classic.Core/Interfaces/` - Domain contracts and abstractions
- `Classic.Infrastructure/Configuration/SettingsService.cs` - Configuration management
