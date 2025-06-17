# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CLASSIC-8 is a C#/.NET 8.0 port of CLASSIC (Crash Log Auto Scanner & Setup Integrity Checker), originally written in Python. The application analyzes crash logs and checks game setup integrity for Bethesda games (Fallout 4, Skyrim SE, and Starfield).

This is a hybrid application supporting both CLI and GUI interfaces, so all scanning and analysis logic should be implemented in a separate class library to ensure it can be used by both interfaces without UI dependencies.

## Build and Run Commands

```bash
# Build the solution
dotnet build

# Run the desktop application
dotnet run --project CLASSIC-8.Desktop

# Build for release
dotnet build -c Release
```

## Architecture

The application uses:
- **UI Framework**: Avalonia UI 11.3.1 with ReactiveUI for MVVM
- **Target**: .NET 8.0 cross-platform desktop application
- **Structure**: 
  - `CLASSIC-8/`: Core application (ViewModels, Views, Assets)
  - `CLASSIC-8.Desktop/`: Desktop launcher project
  - `CLASSIC-8.Core/`: All scanning logic, ensuring clean separation between UI and business logic

## Original Python Application

The Python code to be ported is located in `/Code to Port/`:
- **Main modules**: CLASSIC_Main.py, CLASSIC_Interface.py, CLASSIC_ScanGame.py, CLASSIC_ScanLogs.py
- **Core library**: ClassicLib/ contains various analyzers and utilities
- **Data files**: CLASSIC Data/ contains YAML configurations, graphics, and sounds
- **Crash log files**: Crash Logs/ contains example crash logs for testing and reports for reference.

Key functionality to port:
1. Crash log scanning and analysis for multiple game engines
2. Game file integrity checking (FCX mode)
3. Multi-game support with game-specific configurations
4. YAML-based configuration system
5. Pastebin integration for remote crash log fetching
6. Audio notifications
7. Update checking from GitHub/Nexus

## Development Guidelines

When porting from Python to C#:
- Ensure that all services and components are registered in the .NET dependency injection container.
- Use a singleton pattern for passing global settings and configurations.
- Convert YAML configuration handling to use YamlDotNet or similar
- Replace PySide6 UI components with equivalent Avalonia controls
- Use async/await for background operations (replacing Python's asyncio)
- Implement proper MVVM pattern with ViewModels and data binding
- Generate tests which are compatible with .NET testing frameworks (e.g., xUnit)

## Key Python Dependencies to Replace

- PySide6 → Avalonia UI (already in place)
- ruamel.yaml → YamlDotNet or similar
- aiohttp/requests → HttpClient
- regex → System.Text.RegularExpressions
- pathlib → System.IO.Path
- winreg → Microsoft.Win32.Registry (Conditional for Windows)
- logging → NLog
- pytest → xUnit