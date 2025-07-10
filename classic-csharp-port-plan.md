# CLASSIC C# Port Implementation Plan

## Project Structure Overview

```
ClassicCS/
├── src/
│   ├── Classic.Core/                    # Core domain models and interfaces
│   ├── Classic.ScanLog/                 # Crash log scanning logic
│   ├── Classic.ScanGame/                # Game file scanning logic  
│   ├── Classic.Infrastructure/          # YAML, file I/O, utilities
│   ├── Classic.Avalonia/                # GUI application (existing)
│   └── Classic.CLI/                     # Command-line application
├── tests/
│   ├── Classic.Core.Tests/
│   ├── Classic.ScanLog.Tests/
│   ├── Classic.ScanGame.Tests/
│   └── Classic.Infrastructure.Tests/
└── Classic.sln
```

## Phase 1: Foundation and Infrastructure (Week 1-2)

### 1.1 Core Library Setup
- [ ] Create `Classic.Core` class library project
  - [ ] Define domain models (CrashLog, Plugin, FormID, etc.)
  - [ ] Create core interfaces (IScanOrchestrator, IMessageHandler, etc.)
  - [ ] Define enums (GameID, MessageType, MessageTarget, etc.)
  - [ ] Create custom exceptions
  - [ ] Set up dependency injection container interfaces

### 1.2 Infrastructure Library
- [ ] Create `Classic.Infrastructure` class library project
  - [ ] Implement YAML settings management
    - [ ] Port YamlSettingsCache to C# using YamlDotNet
    - [ ] Create IYamlSettingsCache interface
    - [ ] Implement caching mechanism
  - [ ] Implement file system utilities
    - [ ] Async file I/O operations
    - [ ] Path management utilities
    - [ ] File hash calculation
  - [ ] Implement logging infrastructure
    - [ ] Use Serilog for structured logging
    - [ ] Create logging abstractions
  - [ ] Port MessageHandler system
    - [ ] Create IMessageHandler interface
    - [ ] Implement CLI and GUI message handlers
    - [ ] Create progress reporting system

### 1.3 Configuration Management
- [ ] Port Constants module
  - [ ] Create Constants.cs with all game versions, paths, etc.
  - [ ] Implement configuration loading
- [ ] Port GlobalRegistry pattern
  - [ ] Implement singleton registry using DI
  - [ ] Create service registration extensions

## Phase 2: Core Scanning Logic (Week 3-4)

### 2.1 ScanLog Library Foundation
- [ ] Create `Classic.ScanLog` class library project
- [ ] Port core data structures
  - [ ] ClassicScanLogsInfo → ScanLogConfiguration class
  - [ ] ThreadSafeLogCache → ConcurrentLogCache class
  - [ ] Create crash log models

### 2.2 Parser and Analyzers
- [ ] Port Parser module
  - [ ] Implement crash header parsing
  - [ ] Implement segment extraction
  - [ ] Port regex patterns for log parsing
- [ ] Port analyzer components
  - [ ] PluginAnalyzer class
  - [ ] FormIDAnalyzer class
  - [ ] SuspectScanner class
  - [ ] RecordScanner class
  - [ ] SettingsScanner class
- [ ] Port utility modules
  - [ ] DetectMods functionality
  - [ ] GPUDetector (using WMI or similar)

### 2.3 Orchestrator Implementation
- [ ] Port ScanOrchestrator
  - [ ] Implement IScanOrchestrator interface
  - [ ] Create async workflow coordination
  - [ ] Implement statistics tracking
- [ ] Port AsyncScanOrchestrator
  - [ ] Implement parallel processing with Task.WhenAll
  - [ ] Use Channel<T> for async producer-consumer pattern
  - [ ] Implement batch processing

### 2.4 Report Generation
- [ ] Port ReportGenerator
  - [ ] Implement report formatting
  - [ ] Create report templates
  - [ ] Implement async file writing

## Phase 3: Game Scanning Logic (Week 5)

### 3.1 ScanGame Library
- [ ] Create `Classic.ScanGame` class library project
- [ ] Port game file scanning modules
  - [ ] CheckXsePlugins → XsePluginChecker class
  - [ ] CheckCrashgen → CrashgenChecker class
  - [ ] WryeCheck → WryeBashChecker class
  - [ ] ScanModInis → ModIniScanner class
- [ ] Port configuration file handling
  - [ ] Config file parsing
  - [ ] INI file comparison
  - [ ] TOML support (if needed)

## Phase 4: CLI Application (Week 6)

### 4.1 CLI Project Setup
- [ ] Create `Classic.CLI` console application project
- [ ] Implement command-line parsing
  - [ ] Use System.CommandLine or CommandLineParser
  - [ ] Define command structure
  - [ ] Implement help system

### 4.2 CLI Features
- [ ] Port CLASSIC_ScanLogs functionality
  - [ ] Implement scan command
  - [ ] Add progress reporting
  - [ ] Handle file output
- [ ] Port CLASSIC_ScanGame functionality
  - [ ] Implement game scan command
  - [ ] Add integrity checking
- [ ] Implement CLI-specific features
  - [ ] Batch processing
  - [ ] JSON output format option
  - [ ] Quiet/verbose modes

## Phase 5: GUI Integration (Week 7-8)

### 5.1 ViewModels Implementation
- [ ] Create main ViewModels
  - [ ] MainWindowViewModel
  - [ ] ScanLogsViewModel
  - [ ] ScanGameViewModel
  - [ ] SettingsViewModel
  - [ ] BackupViewModel
- [ ] Implement ReactiveUI patterns
  - [ ] Use ReactiveCommand for async operations
  - [ ] Implement INotifyPropertyChanged via ReactiveObject
  - [ ] Create Observable collections for lists

### 5.2 Views and Controls
- [ ] Port main window layout
  - [ ] Create tab control structure
  - [ ] Implement scan buttons
  - [ ] Add progress indicators
- [ ] Create custom controls
  - [ ] Log viewer control
  - [ ] Settings editor
  - [ ] File/folder pickers
- [ ] Implement dialogs
  - [ ] Error dialogs
  - [ ] Progress dialogs
  - [ ] Help dialogs

### 5.3 Service Integration
- [ ] Wire up DI container
  - [ ] Register all services
  - [ ] Configure service lifetimes
  - [ ] Implement factory patterns where needed
- [ ] Implement background tasks
  - [ ] Use IHostedService for monitoring
  - [ ] Implement cancellation tokens
  - [ ] Handle cross-thread operations

## Phase 6: Advanced Features (Week 9)

### 6.1 Async Pipeline
- [ ] Port async pipeline implementation
  - [ ] Use System.Threading.Channels
  - [ ] Implement producer-consumer patterns
  - [ ] Add performance monitoring
- [ ] Optimize parallel processing
  - [ ] Use Parallel.ForEachAsync
  - [ ] Implement batch processing
  - [ ] Add configurable concurrency

### 6.2 Database Integration
- [ ] Port FormID database handling
  - [ ] Implement async database pool
  - [ ] Use SQLite with Dapper or EF Core
  - [ ] Add caching layer
- [ ] Implement mod detection database
  - [ ] Port YAML mod databases
  - [ ] Create efficient lookup structures

## Phase 7: Testing and Polish (Week 10)

### 7.1 Unit Tests
- [ ] Create comprehensive test suites
  - [ ] Parser tests
  - [ ] Analyzer tests
  - [ ] Orchestrator tests
  - [ ] Utility function tests
- [ ] Implement integration tests
  - [ ] End-to-end scanning tests
  - [ ] File I/O tests
  - [ ] Database tests

### 7.2 Performance Optimization
- [ ] Profile application performance
  - [ ] Identify bottlenecks
  - [ ] Optimize file I/O
  - [ ] Improve memory usage
- [ ] Implement caching strategies
  - [ ] Cache parsed logs
  - [ ] Cache YAML configurations
  - [ ] Implement lazy loading

### 7.3 Final Polish
- [ ] Error handling improvements
  - [ ] Add comprehensive try-catch blocks
  - [ ] Implement retry logic
  - [ ] Create user-friendly error messages
- [ ] Documentation
  - [ ] XML documentation for public APIs
  - [ ] Create developer guide
  - [ ] Update user documentation

## Key Technical Considerations

### Dependency Injection
```csharp
services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
services.AddScoped<IScanOrchestrator, AsyncScanOrchestrator>();
services.AddTransient<IFormIDAnalyzer, FormIDAnalyzer>();
```

### Async/Await Patterns
```csharp
public async Task<ScanResult> ProcessCrashLogAsync(
    string logPath, 
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### MVVM with ReactiveUI
```csharp
public class ScanLogsViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> ScanCommand { get; }
    
    private readonly ObservableAsPropertyHelper<bool> _isScanning;
    public bool IsScanning => _isScanning.Value;
}
```

### Configuration Pattern
```csharp
public class ScanConfiguration
{
    public bool UseAsyncPipeline { get; set; } = true;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
```

## Development Guidelines

1. **Naming Conventions**
   - Use PascalCase for public members
   - Use camelCase for private fields
   - Prefix interfaces with 'I'

2. **Async Best Practices**
   - Always use ConfigureAwait(false) in library code
   - Provide cancellation token support
   - Use ValueTask where appropriate

3. **Error Handling**
   - Create domain-specific exceptions
   - Log errors with structured logging
   - Provide meaningful error messages

4. **Testing Strategy**
   - Aim for >80% code coverage
   - Use xUnit for unit tests
   - Mock external dependencies

5. **Performance Considerations**
   - Use object pooling for frequent allocations
   - Implement lazy loading for large datasets
   - Profile memory usage regularly

## Migration Notes

- Python's `asyncio` → C# `Task` and `async/await`
- Python's `multiprocessing` → C# `Parallel` class or `Task.Run`
- PySide6 signals → ReactiveUI observables
- YAML parsing → YamlDotNet library
- Regular expressions → .NET Regex with compiled option
- File paths → Use `Path.Combine` and `Path.GetFullPath`

## Success Criteria

- [ ] All core functionality ported and working
- [ ] Performance equal or better than Python version
- [ ] Comprehensive test coverage
- [ ] Clean separation of concerns
- [ ] Well-documented codebase
- [ ] Smooth user experience in both CLI and GUI