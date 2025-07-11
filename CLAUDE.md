# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CLASSIC-8 is a C# port of a Python crash log analysis tool for Bethesda games (Fallout 4, Skyrim SE, Skyrim VR, Fallout 4 VR). The application scans and analyzes game crash logs to identify mod conflicts, plugin problems, and other issues.

## Technology Stack

- **.NET 8.0** with C# 12
- **Avalonia UI 11.3.2** for cross-platform desktop GUI
- **ReactiveUI** for MVVM pattern implementation
- **Serilog** for structured logging
- **YamlDotNet** for configuration management
- **Microsoft.Extensions.*** for dependency injection and configuration

## Build Commands

```bash
# Build the entire solution
dotnet build

# Build specific projects
dotnet build Classic.Avalonia/Classic.Avalonia.csproj
dotnet build Classic.Core/Classic.Core.csproj
dotnet build Classic.Infrastructure/Classic.Infrastructure.csproj

# Run the GUI application
dotnet run --project Classic.Avalonia

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Project Architecture

The solution follows clean architecture with three main layers:

### Classic.Core
Domain layer containing:
- **Models**: CrashLog, Plugin, FormID entities
- **Interfaces**: IScanOrchestrator, IMessageHandler, IYamlSettingsCache
- **Enums**: GameID, MessageType, MessageTarget
- **Exceptions**: Custom domain exceptions

### Classic.Infrastructure  
Infrastructure layer containing:
- **YAML Configuration**: Settings management and caching
- **File System**: Utilities and abstractions for file operations
- **Logging**: Serilog configuration and structured logging
- **Message Handling**: Async message processing system
- **Dependency Injection**: Service registration extensions

### Classic.Avalonia
Presentation layer containing:
- **MVVM Pattern**: ViewModels using ReactiveUI
- **Views**: Avalonia XAML user interfaces
- **Cross-platform**: Desktop GUI for Windows, macOS, Linux

## Development Patterns

### Async/Await Usage
- Use `async/await` extensively for file I/O and processing
- Implement producer-consumer patterns with `System.Threading.Channels`
- Replace Python's asyncio patterns with .NET async equivalents

### Dependency Injection
- Register services in `Classic.Infrastructure` using Microsoft.Extensions.DI
- Use constructor injection in ViewModels and services
- Abstract file system operations using `System.IO.Abstractions`

### Configuration Management
- YAML-based configuration using YamlDotNet
- Implement caching for frequently accessed settings
- Support runtime configuration updates

### Error Handling
- Use structured logging with Serilog for all operations
- Implement custom exceptions in the Core project
- Provide meaningful error messages for crash log analysis failures

## Testing Strategy

Currently no test projects exist. When implementing tests:
- Create unit tests for Core domain logic
- Use `System.IO.Abstractions.TestingHelpers` for file system mocking
- Test YAML configuration parsing and validation
- Mock external dependencies in Infrastructure layer tests

## Migration Notes

This is a port from a Python application. Key considerations:
- Python's asyncio → .NET async/await patterns
- PySide6 GUI → Avalonia UI with ReactiveUI
- Python's yaml library → YamlDotNet
- File operations → System.IO.Abstractions for testability

## Development Status

The project is in early development with placeholder implementations. Focus areas:
1. Implement Core domain models and interfaces
2. Build YAML configuration system in Infrastructure
3. Create message handling and async processing infrastructure
4. Develop crash log parsing and analysis logic
5. Implement Avalonia UI with proper MVVM patterns

## Code Location

- The code we are porting is in the "Code to Port" directory.

## Logging Guidelines

- This project standardized on Serilog for its logging framework, Microsoft.Extensions.Logging should not be used.