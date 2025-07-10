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
- [x] Create `Classic.Core` class library project
  - [x] Define domain models (CrashLog, Plugin, FormID, ScanStatistics, Suspect)
  - [x] Create core interfaces (IScanOrchestrator, IMessageHandler, IYamlSettingsCache, IFormIDAnalyzer, IPluginAnalyzer, ICrashLogParser, IGlobalRegistry)
  - [x] Define enums (GameID, MessageType, MessageTarget, PluginStatus, SuspectType, SeverityLevel)
  - [x] Create custom exceptions (ClassicException, CrashLogParsingException, ConfigurationException, ScanningException)
  - [x] Set up dependency injection container interfaces

### 1.2 Infrastructure Library
- [x] Create `Classic.Infrastructure` class library project
  - [x] Implement YAML settings management
    - [x] Port YamlSettingsCache to C# using YamlDotNet
    - [x] Create IYamlSettingsCache interface
    - [x] Implement caching mechanism
  - [x] Implement file system utilities
    - [x] Async file I/O operations
    - [x] Path management utilities
    - [x] File hash calculation
  - [x] Implement logging infrastructure
    - [x] Use Serilog for structured logging
    - [x] Create logging abstractions
  - [x] Port MessageHandler system
    - [x] Create IMessageHandler interface
    - [x] Implement CLI and GUI message handlers
    - [x] Create progress reporting system

### 1.3 Configuration Management
- [x] Port Constants module
  - [x] Create Constants.cs with all game versions, paths, regex patterns, configuration
  - [x] Implement configuration loading
- [x] Port GlobalRegistry pattern
  - [x] Implement singleton registry using DI
  - [x] Create service registration extensions

## Phase 2: Core Scanning Logic (Week 3-4)

### 2.1 ScanLog Library Foundation
- [x] Create `Classic.ScanLog` class library project
- [x] Port core data structures
  - [x] ClassicScanLogsInfo → ScanLogConfiguration class
  - [x] ThreadSafeLogCache → ConcurrentLogCache class
  - [x] Create crash log models

### 2.2 Parser and Analyzers
- [x] Port Parser module
  - [x] Implement crash header parsing
  - [x] Implement segment extraction
  - [x] Port regex patterns for log parsing
- [x] Port analyzer components
  - [x] PluginAnalyzer class
  - [x] FormIDAnalyzer class
  - [x] SuspectScanner class
  - [x] RecordScanner class
  - [x] SettingsScanner class
- [x] Port utility modules
  - [x] DetectMods functionality
  - [x] GPUDetector (using WMI or similar with fallback if unavailable)

### 2.3 Unified Orchestrator Implementation
- [x] Create adaptive ScanOrchestrator
  - [x] Implement IScanOrchestrator interface with configurable processing modes
  - [x] Create async workflow coordination with performance monitoring
  - [x] Implement statistics tracking and performance metrics collection
- [x] Implement multiple processing strategies
  - [x] Sequential processing for single-threaded scenarios
  - [x] Parallel processing with Task.WhenAll for CPU-bound operations
  - [x] Producer-consumer pattern using Channel<T> for I/O-bound operations
  - [x] Batch processing with configurable batch sizes
- [x] Add adaptive strategy selection
  - [x] Performance benchmarking during initialization
  - [x] Dynamic strategy switching based on workload characteristics
  - [x] Configuration-driven strategy selection with fallback options

### 2.4 Report Generation
- [x] Port ReportGenerator
  - [x] Implement report formatting
  - [x] Create report templates
  - [x] Implement async file writing

## Phase 3: Game Scanning Logic (Week 5)

### 3.1 ScanGame Library
- [x] Create `Classic.ScanGame` class library project
- [x] Port game file scanning modules
  - [x] CheckXsePlugins → XsePluginChecker class
  - [x] CheckCrashgen → CrashgenChecker class
  - [x] WryeCheck → WryeBashChecker class
  - [x] ScanModInis → ModIniScanner class
- [x] Port configuration file handling
  - [x] Config file parsing
  - [x] INI file comparison
  - [x] TOML support (if needed)

## Phase 4: CLI Application (Week 6)

### 4.1 CLI Project Setup
- [x] Create `Classic.CLI` console application project
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

### Unified Orchestrator Pattern
```csharp
public class AdaptiveScanOrchestrator : IScanOrchestrator
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private ProcessingStrategy _currentStrategy;
    
    public async Task<ScanResult> ExecuteScanAsync(ScanRequest request)
    {
        // Select optimal strategy based on workload and performance metrics
        var strategy = await SelectOptimalStrategyAsync(request);
        return await strategy.ExecuteAsync(request);
    }
    
    private async Task<ProcessingStrategy> SelectOptimalStrategyAsync(ScanRequest request)
    {
        // Performance-based strategy selection logic
    }
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
    public ProcessingMode PreferredMode { get; set; } = ProcessingMode.Adaptive;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int BatchSize { get; set; } = 100;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public TimeSpan StrategyEvaluationInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public enum ProcessingMode
{
    Sequential,      // Single-threaded processing
    Parallel,        // Task.WhenAll parallel processing
    ProducerConsumer, // Channel-based pipeline
    Adaptive         // Auto-select based on performance
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
