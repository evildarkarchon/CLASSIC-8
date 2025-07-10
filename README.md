# CLASSIC-8 C# Port

A C# port of the CLASSIC crash log analysis tool for Bethesda games (Fallout 4, Skyrim SE, Skyrim VR, Fallout 4 VR).

## Current Status: Phase 1 Complete ✅

### Phase 1 Implementation Summary

#### ✅ Core Library (`Classic.Core`)
- **Domain Models**: CrashLog, Plugin, FormID, ScanStatistics, Suspect
- **Core Interfaces**: IScanOrchestrator, IMessageHandler, IYamlSettingsCache, IFormIDAnalyzer, IPluginAnalyzer, ICrashLogParser, IGlobalRegistry
- **Enums**: GameID, MessageType, MessageTarget, PluginStatus, SuspectType, SeverityLevel
- **Custom Exceptions**: ClassicException, CrashLogParsingException, ConfigurationException, ScanningException

#### ✅ Infrastructure Library (`Classic.Infrastructure`)
- **YAML Configuration**: YamlSettingsCache with caching and async support using YamlDotNet
- **Message Handling**: MessageHandlerBase, ConsoleMessageHandler, GuiMessageHandler with progress reporting
- **File System Utilities**: Async file I/O operations, hash calculation, crash log validation
- **Logging**: Serilog configuration with console and file output
- **Global Registry**: Service registry pattern with DI integration
- **Dependency Injection**: Service registration extensions
- **Enhanced Constants**: Comprehensive game versions, paths, regex patterns, and configuration

#### ✅ Testing Infrastructure
- **Test Projects**: Classic.Core.Tests, Classic.Infrastructure.Tests
- **Testing Packages**: xUnit, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers
- **Initial Tests**: Domain model tests, message handler tests
- **Test Coverage**: 12 passing tests across core and infrastructure components

#### ✅ Development Environment
- **Configuration Files**: global.json (.NET 8.0), .editorconfig (code style)
- **Project Structure**: Clean architecture with proper separation of concerns
- **Build System**: All projects build successfully with minimal warnings

## Architecture

```
CLASSIC-8/
├── Classic.Core/              # Domain models and interfaces
├── Classic.Infrastructure/    # YAML, messaging, file I/O, logging
├── Classic.Avalonia/         # GUI application (existing)
├── Classic.CLI/              # Command-line application (existing)
├── Classic.ScanLog/          # Crash log scanning logic (placeholder)
├── Classic.ScanGame/         # Game file scanning logic (placeholder)
├── tests/
│   ├── Classic.Core.Tests/
│   └── Classic.Infrastructure.Tests/
└── docs/                     # Documentation and implementation plans
```

## Technology Stack

- **.NET 8.0** with C# 12
- **Avalonia UI 11.3.2** for cross-platform desktop GUI
- **ReactiveUI** for MVVM pattern implementation
- **Serilog** for structured logging
- **YamlDotNet** for configuration management
- **Microsoft.Extensions.*** for dependency injection and configuration
- **xUnit, FluentAssertions, Moq** for testing

## Build Commands

```bash
# Build Core library
dotnet build Classic.Core/

# Build Infrastructure library
dotnet build Classic.Infrastructure/

# Run tests
dotnet test tests/Classic.Core.Tests/
dotnet test tests/Classic.Infrastructure.Tests/

# Run GUI application
dotnet run --project Classic.Avalonia

# Run CLI application
dotnet run --project Classic.CLI
```

## Next Steps: Phase 2

The foundation is now complete. Phase 2 will focus on implementing the core scanning logic:

1. **Crash Log Parser** - Parse crash log files and extract segments
2. **Analyzers** - FormID analysis, plugin conflict detection
3. **Scan Orchestrator** - Coordinate the analysis workflow with async processing
4. **Report Generation** - Generate formatted analysis reports

## Key Design Decisions

- **Clean Architecture**: Clear separation between domain, infrastructure, and presentation layers
- **Async-First**: All I/O operations use async/await patterns
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service registration
- **Cross-Platform**: Targeting .NET 8.0 for Windows, macOS, and Linux support
- **Structured Logging**: Serilog for comprehensive logging and debugging
- **Message Handling**: Abstracted message system supporting both CLI and GUI output
- **Testability**: Interfaces and abstractions to enable comprehensive unit testing

## Contributing

This project follows the implementation plan outlined in `docs/classic-csharp-port-plan.md`. See the detailed phase 1 checklist in `docs/phase1-detailed-checklist.md` for completed items.
