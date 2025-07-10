# C# Architecture Patterns for CLASSIC Port

## Key Patterns to Implement

### 1. Async/Await Pattern (Replacing Python's asyncio)

**Python Pattern:**
```python
async def process_crash_logs_batch_async(self, crashlog_files: list[Path]) -> list[ScanResult]:
    tasks = [self._process_single_log_async(log) for log in crashlog_files]
    return await asyncio.gather(*tasks)
```

**C# Equivalent:**
```csharp
public async Task<List<ScanResult>> ProcessCrashLogsBatchAsync(
    IEnumerable<string> crashLogFiles, 
    CancellationToken cancellationToken = default)
{
    var tasks = crashLogFiles.Select(log => ProcessSingleLogAsync(log, cancellationToken));
    return await Task.WhenAll(tasks);
}
```

### 2. Producer-Consumer Pattern (Using Channels)

**Python Pattern:**
```python
async def integrate_async_file_loading(self):
    queue = asyncio.Queue()
    # Producer/consumer logic
```

**C# Equivalent:**
```csharp
public async Task ProcessWithChannelsAsync(CancellationToken cancellationToken)
{
    var channel = Channel.CreateUnbounded<CrashLog>();
    
    // Producer
    var producer = Task.Run(async () =>
    {
        await foreach (var file in GetFilesAsync())
        {
            await channel.Writer.WriteAsync(file, cancellationToken);
        }
        channel.Writer.Complete();
    });
    
    // Consumer
    var consumer = Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await ProcessLogAsync(item);
        }
    });
    
    await Task.WhenAll(producer, consumer);
}
```

### 3. Dependency Injection Pattern

**Service Registration:**
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Singleton - single instance for app lifetime
        services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
        services.AddSingleton<IGlobalRegistry, GlobalRegistry>();
        
        // Scoped - new instance per request/operation
        services.AddScoped<IScanOrchestrator, AsyncScanOrchestrator>();
        
        // Transient - new instance every time
        services.AddTransient<IFormIDAnalyzer, FormIDAnalyzer>();
        services.AddTransient<IPluginAnalyzer, PluginAnalyzer>();
        
        // Factory pattern for complex initialization
        services.AddSingleton<Func<MessageTarget, IMessageHandler>>(provider => target =>
        {
            return target switch
            {
                MessageTarget.CLI => provider.GetRequiredService<ConsoleMessageHandler>(),
                MessageTarget.GUI => provider.GetRequiredService<GuiMessageHandler>(),
                _ => throw new ArgumentException($"Unknown target: {target}")
            };
        });
    }
}
```

### 4. MVVM with ReactiveUI Pattern

**ViewModel Example:**
```csharp
public class ScanLogsViewModel : ReactiveObject
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly ObservableAsPropertyHelper<bool> _isScanning;
    private string _currentStatus;
    
    public ScanLogsViewModel(IScanOrchestrator scanOrchestrator)
    {
        _scanOrchestrator = scanOrchestrator;
        
        // Create command with async execution
        ScanCommand = ReactiveCommand.CreateFromTask(
            ExecuteScanAsync,
            this.WhenAnyValue(x => x.IsScanning).Select(x => !x));
        
        // Track scanning state
        _isScanning = ScanCommand.IsExecuting
            .ToProperty(this, x => x.IsScanning);
        
        // Handle errors
        ScanCommand.ThrownExceptions
            .Subscribe(ex => this.Log().Error(ex, "Scan failed"));
    }
    
    public ReactiveCommand<Unit, Unit> ScanCommand { get; }
    public bool IsScanning => _isScanning.Value;
    
    public string CurrentStatus
    {
        get => _currentStatus;
        set => this.RaiseAndSetIfChanged(ref _currentStatus, value);
    }
    
    private async Task ExecuteScanAsync(CancellationToken cancellationToken)
    {
        CurrentStatus = "Starting scan...";
        var results = await _scanOrchestrator.ProcessCrashLogsBatchAsync(
            GetLogFiles(), 
            cancellationToken);
        CurrentStatus = $"Scan complete. Processed {results.Count} logs.";
    }
}
```

### 5. Repository Pattern with Caching

```csharp
public interface IFormIDRepository
{
    Task<FormID> GetByValueAsync(string value);
    Task<IEnumerable<FormID>> GetByPluginAsync(string pluginName);
}

public class CachedFormIDRepository : IFormIDRepository
{
    private readonly IFormIDRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    
    public async Task<FormID> GetByValueAsync(string value)
    {
        return await _cache.GetOrCreateAsync($"formid:{value}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            return await _innerRepository.GetByValueAsync(value);
        });
    }
}
```

### 6. Progress Reporting Pattern

```csharp
public class ProgressContext : IDisposable
{
    private readonly IProgress<ProgressInfo> _progress;
    private readonly string _operation;
    private int _current;
    private readonly int _total;
    
    public ProgressContext(string operation, int total, IProgress<ProgressInfo> progress)
    {
        _operation = operation;
        _total = total;
        _progress = progress;
        _current = 0;
    }
    
    public void Report(string message = null)
    {
        _current++;
        _progress?.Report(new ProgressInfo
        {
            Operation = _operation,
            Current = _current,
            Total = _total,
            Message = message,
            Percentage = (_current * 100.0) / _total
        });
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

### 7. Async Enumerable Pattern

```csharp
public async IAsyncEnumerable<CrashLog> ReadCrashLogsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    foreach (var file in Directory.GetFiles(_crashLogPath, "*.log"))
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var content = await File.ReadAllLinesAsync(file, cancellationToken);
        var crashLog = ParseCrashLog(file, content);
        
        yield return crashLog;
    }
}

// Usage
await foreach (var log in ReadCrashLogsAsync(cancellationToken))
{
    await ProcessLogAsync(log);
}
```

### 8. Options Pattern for Configuration

```csharp
public class ScanOptions
{
    public const string SectionName = "Scanning";
    
    public bool UseAsyncPipeline { get; set; } = true;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public bool ShowFormIDValues { get; set; } = false;
    public string CustomScanPath { get; set; }
}

// Registration
services.Configure<ScanOptions>(configuration.GetSection(ScanOptions.SectionName));

// Usage
public class ScanOrchestrator
{
    private readonly ScanOptions _options;
    
    public ScanOrchestrator(IOptions<ScanOptions> options)
    {
        _options = options.Value;
    }
}
```

### 9. Result Pattern for Error Handling

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess 
            ? Result<TNew>.Success(mapper(Value)) 
            : Result<TNew>.Failure(Error);
    }
}

// Usage
public async Task<Result<ScanReport>> ScanLogAsync(string path)
{
    try
    {
        if (!File.Exists(path))
            return Result<ScanReport>.Failure("File not found");
            
        var content = await File.ReadAllTextAsync(path);
        var report = ParseLog(content);
        
        return Result<ScanReport>.Success(report);
    }
    catch (Exception ex)
    {
        return Result<ScanReport>.Failure($"Failed to scan: {ex.Message}");
    }
}
```

### 10. Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IFormIDRepository FormIDs { get; }
    IPluginRepository Plugins { get; }
    ISuspectRepository Suspects { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private IDbContextTransaction _transaction;
    
    public IFormIDRepository FormIDs { get; }
    public IPluginRepository Plugins { get; }
    public ISuspectRepository Suspects { get; }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitAsync()
    {
        await _transaction?.CommitAsync();
    }
}
```

## Best Practices

### 1. Cancellation Token Usage
Always pass cancellation tokens through async call chains:
```csharp
public async Task ProcessAsync(CancellationToken cancellationToken = default)
{
    await FirstStepAsync(cancellationToken);
    await SecondStepAsync(cancellationToken);
}
```

### 2. ConfigureAwait in Libraries
Use `ConfigureAwait(false)` in library code:
```csharp
public async Task<string> ReadFileAsync(string path)
{
    return await File.ReadAllTextAsync(path).ConfigureAwait(false);
}
```

### 3. Async Disposal
Implement IAsyncDisposable for async cleanup:
```csharp
public class AsyncScanner : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    
    public async ValueTask DisposeAsync()
    {
        if (_httpClient != null)
        {
            _httpClient.Dispose();
        }
        
        await CleanupAsync().ConfigureAwait(false);
    }
}
```

### 4. Lazy Initialization
Use Lazy<T> for expensive operations:
```csharp
public class DatabaseService
{
    private readonly Lazy<Task<IDatabase>> _database;
    
    public DatabaseService()
    {
        _database = new Lazy<Task<IDatabase>>(InitializeDatabaseAsync);
    }
    
    public Task<IDatabase> GetDatabaseAsync() => _database.Value;
}
```

### 5. Memory-Efficient String Building
Use StringBuilder or StringWriter for report generation:
```csharp
public string GenerateReport(ScanResult result)
{
    using var writer = new StringWriter();
    writer.WriteLine($"Scan Report - {DateTime.Now}");
    writer.WriteLine(new string('=', 50));
    
    foreach (var finding in result.Findings)
    {
        writer.WriteLine($"- {finding.Severity}: {finding.Message}");
    }
    
    return writer.ToString();
}
```