# Standardized Serilog Usage Guide

## What Changed

The project has been standardized to use **Serilog** exclusively for all logging instead of mixing `Microsoft.Extensions.Logging` with Serilog. This provides:

- **Consistency**: All components use the same logging interface
- **Rich Logging**: Direct access to Serilog's structured logging features
- **Performance**: No abstraction layer overhead
- **Simplicity**: Single logging dependency

## Package Changes

### Removed Packages:
- `Microsoft.Extensions.Logging` (from Classic.Infrastructure)
- `Microsoft.Extensions.Logging.Abstractions` (from Classic.Core and test projects)

### Kept Packages:
- `Serilog` ✅
- `Serilog.Sinks.Console` ✅
- `Serilog.Sinks.File` ✅

## Code Changes

### Before (Microsoft.Extensions.Logging):
```csharp
using Microsoft.Extensions.Logging;

public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.LogInformation("Processing {Count} items", 42);
        _logger.LogError(ex, "Error processing item {Id}", itemId);
    }
}
```

### After (Pure Serilog):
```csharp
using Serilog;

public class MyService
{
    private readonly ILogger _logger;
    
    public MyService(ILogger logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.Information("Processing {Count} items", 42);
        _logger.Error(ex, "Error processing item {Id}", itemId);
    }
}
```

## Dependency Injection Registration

The service registration has been updated in `ServiceCollectionExtensions`:

```csharp
public static IServiceCollection AddClassicInfrastructure(this IServiceCollection services)
{
    // Configure Serilog as the global logger
    Log.Logger = LoggingConfiguration.CreateLogger();
    
    // Register Serilog logger as singleton
    services.AddSingleton<Serilog.ILogger>(Log.Logger);
    
    // Other services automatically get Serilog.ILogger injected
    services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
    services.AddSingleton<ConsoleMessageHandler>();
    services.AddSingleton<GuiMessageHandler>();
    
    return services;
}
```

## Benefits

### 1. **Structured Logging**
Direct access to Serilog's powerful structured logging:
```csharp
_logger.Information("User {UserId} performed {Action} on {Resource}", 
    userId, "UPDATE", resourceName);
```

### 2. **Rich Context**
Easy context enrichment:
```csharp
using (_logger.BeginScope("Processing batch {BatchId}", batchId))
{
    _logger.Information("Started processing");
    // All log messages in this scope will include BatchId
}
```

### 3. **Flexible Configuration**
Direct access to Serilog's configuration in `LoggingConfiguration.cs`:
```csharp
return new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/classic-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### 4. **Testing**
Simplified mocking with Serilog's `ILogger`:
```csharp
public class MyServiceTests
{
    private readonly Mock<ILogger> _mockLogger;
    
    [Test]
    public void TestMethod()
    {
        // Verify specific log calls
        _mockLogger.Verify(
            x => x.Information<string, int>(
                "Processing {Count} items", 
                It.IsAny<int>()),
            Times.Once);
    }
}
```

## Migration Summary

✅ **All logging standardized on Serilog**  
✅ **Microsoft.Extensions.Logging packages removed**  
✅ **All message handlers updated**  
✅ **YAML settings cache updated**  
✅ **Global registry updated**  
✅ **Tests updated and passing**  
✅ **Service registration updated**  

The codebase now has a single, consistent logging approach that provides better performance, more features, and simpler maintenance.
