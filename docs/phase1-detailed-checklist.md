# Phase 1: Detailed Implementation Checklist

## Initial Project Setup

### Solution Structure Creation
- [x] Create new solution: `dotnet new sln -n Classic`
- [x] Create folder structure:
  ```bash
  mkdir src
  mkdir tests
  mkdir docs
  ```

### Core Projects
- [x] Create Classic.Core project:
  ```bash
  dotnet new classlib -n Classic.Core -o src/Classic.Core
  dotnet sln add src/Classic.Core/Classic.Core.csproj
  ```
- [x] Create Classic.Infrastructure project:
  ```bash
  dotnet new classlib -n Classic.Infrastructure -o src/Classic.Infrastructure
  dotnet sln add src/Classic.Infrastructure/Classic.Infrastructure.csproj
  ```
- [x] Add project references:
  ```bash
  cd src/Classic.Infrastructure
  dotnet add reference ../Classic.Core/Classic.Core.csproj
  ```

### NuGet Packages Installation

#### Classic.Core packages:
- [x] `dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions`
- [x] `dotnet add package Microsoft.Extensions.Logging.Abstractions`
- [x] `dotnet add package System.ComponentModel.Annotations`

#### Classic.Infrastructure packages:
- [x] `dotnet add package YamlDotNet`
- [x] `dotnet add package Serilog`
- [x] `dotnet add package Serilog.Sinks.Console`
- [x] `dotnet add package Serilog.Sinks.File`
- [x] `dotnet add package Microsoft.Extensions.DependencyInjection`
- [x] `dotnet add package Microsoft.Extensions.Configuration`
- [x] `dotnet add package Microsoft.Extensions.Configuration.Yaml`
- [x] `dotnet add package System.IO.Abstractions`

## Classic.Core Implementation

### Domain Models
- [x] Create `Models` folder
- [x] Implement `CrashLog.cs`:
  ```csharp
  namespace Classic.Core.Models;
  
  public class CrashLog
  {
      public string FileName { get; set; }
      public string FilePath { get; set; }
      public DateTime DateCreated { get; set; }
      public string GameVersion { get; set; }
      public string CrashGenVersion { get; set; }
      public string MainError { get; set; }
      public List<string> RawContent { get; set; }
      public Dictionary<string, List<string>> Segments { get; set; }
  }
  ```

- [x] Implement `Plugin.cs`:
  ```csharp
  public class Plugin
  {
      public string Name { get; set; }
      public string LoadOrder { get; set; }
      public bool IsLightPlugin { get; set; }
      public PluginStatus Status { get; set; }
  }
  ```

- [x] Implement `FormID.cs`:
  ```csharp
  public class FormID
  {
      public string Value { get; set; }
      public string PluginName { get; set; }
      public string ModName { get; set; }
  }
  ```

### Enums
- [x] Create `Enums` folder
- [x] Implement `GameID.cs`:
  ```csharp
  public enum GameID
  {
      Fallout4,
      Fallout4VR,
      SkyrimSE,
      SkyrimVR
  }
  ```

- [x] Implement `MessageType.cs`:
  ```csharp
  public enum MessageType
  {
      Info,
      Warning,
      Error,
      Critical,
      Success,
      Debug
  }
  ```

- [x] Implement `MessageTarget.cs`:
  ```csharp
  [Flags]
  public enum MessageTarget
  {
      None = 0,
      CLI = 1,
      GUI = 2,
      Both = CLI | GUI
  }
  ```

### Interfaces
- [x] Create `Interfaces` folder
- [x] Implement `IMessageHandler.cs`:
  ```csharp
  public interface IMessageHandler
  {
      void SendMessage(string message, MessageType type, MessageTarget target);
      void ReportProgress(string operation, int current, int total);
      IDisposable BeginProgressContext(string operation, int total);
  }
  ```

- [ ] Implement `IScanOrchestrator.cs`:
  ```csharp
  public interface IScanOrchestrator
  {
      Task<ScanResult> ProcessCrashLogAsync(string logPath, CancellationToken cancellationToken = default);
      Task<BatchScanResult> ProcessCrashLogsBatchAsync(IEnumerable<string> logPaths, CancellationToken cancellationToken = default);
  }
  ```

- [x] Implement `IYamlSettingsCache.cs`:
  ```csharp
  public interface IYamlSettingsCache
  {
      T GetSetting<T>(string file, string path, T defaultValue = default);
      Task<T> GetSettingAsync<T>(string file, string path, T defaultValue = default);
      void ReloadCache();
  }
  ```

### Custom Exceptions
- [x] Create `Exceptions` folder
- [x] Implement `ClassicException.cs`:
  ```csharp
  public class ClassicException : Exception
  {
      public ClassicException(string message) : base(message) { }
      public ClassicException(string message, Exception innerException) 
          : base(message, innerException) { }
  }
  ```

- [x] Implement specific exceptions:
  ```csharp
  public class CrashLogParsingException : ClassicException { }
  public class ConfigurationException : ClassicException { }
  public class ScanningException : ClassicException { }
  ```

## Classic.Infrastructure Implementation

### Constants
- [x] Port `Constants.cs`:
  ```csharp
  namespace Classic.Infrastructure;
  
  public static class Constants
  {
      public static class Versions
      {
          public static readonly Version OG_VERSION = new("1.5.97.0");
          public static readonly Version NG_VERSION = new("1.6.1170.0");
          public static readonly Version VR_VERSION = new("1.4.15.0");
      }
      
      public static class Paths
      {
          public const string YAML_FOLDER = "CLASSIC Data/databases";
          public const string CRASH_LOGS_FOLDER = "Crash Logs";
      }
  }
  ```

### YAML Settings Implementation
- [x] Create `Configuration` folder
- [x] Implement `YamlSettingsCache.cs`:
  ```csharp
  public class YamlSettingsCache : IYamlSettingsCache
  {
      private readonly ILogger<YamlSettingsCache> _logger;
      private readonly ConcurrentDictionary<string, object> _cache;
      private readonly IDeserializer _deserializer;
      
      public YamlSettingsCache(ILogger<YamlSettingsCache> logger)
      {
          _logger = logger;
          _cache = new ConcurrentDictionary<string, object>();
          _deserializer = new DeserializerBuilder()
              .WithNamingConvention(UnderscoredNamingConvention.Instance)
              .Build();
      }
  }
  ```

### Message Handler
- [x] Create `Messaging` folder
- [ ] Implement `MessageHandlerBase.cs`:
  ```csharp
  public abstract class MessageHandlerBase : IMessageHandler
  {
      protected readonly ILogger<MessageHandlerBase> Logger;
      
      public abstract void SendMessage(string message, MessageType type, MessageTarget target);
      public abstract void ReportProgress(string operation, int current, int total);
      public abstract IDisposable BeginProgressContext(string operation, int total);
  }
  ```

- [x] Implement `ConsoleMessageHandler.cs`:
  ```csharp
  public class ConsoleMessageHandler : MessageHandlerBase
  {
      public override void SendMessage(string message, MessageType type, MessageTarget target)
      {
          if (!target.HasFlag(MessageTarget.CLI)) return;
          
          var color = type switch
          {
              MessageType.Error => ConsoleColor.Red,
              MessageType.Warning => ConsoleColor.Yellow,
              MessageType.Success => ConsoleColor.Green,
              _ => ConsoleColor.White
          };
          
          Console.ForegroundColor = color;
          Console.WriteLine($"[{type}] {message}");
          Console.ResetColor();
      }
  }
  ```

### File System Utilities
- [x] Create `IO` folder
- [ ] Implement `FileUtilities.cs`:
  ```csharp
  public static class FileUtilities
  {
      public static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
      {
          var lines = new List<string>();
          using var reader = new StreamReader(path);
          string line;
          while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
          {
              lines.Add(line);
          }
          return lines.ToArray();
      }
      
      public static async Task<string> CalculateFileHashAsync(string path, CancellationToken cancellationToken = default)
      {
          using var sha256 = SHA256.Create();
          using var stream = File.OpenRead(path);
          var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
          return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
      }
  }
  ```

### Logging Setup
- [x] Create `Logging` folder
- [ ] Implement `LoggingConfiguration.cs`:
  ```csharp
  public static class LoggingConfiguration
  {
      public static ILogger CreateLogger()
      {
          return new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.Console()
              .WriteTo.File("logs/classic-.txt", rollingInterval: RollingInterval.Day)
              .CreateLogger();
      }
  }
  ```

### Dependency Injection Extensions
- [ ] Create `Extensions` folder
- [ ] Implement `ServiceCollectionExtensions.cs`:
  ```csharp
  public static class ServiceCollectionExtensions
  {
      public static IServiceCollection AddClassicCore(this IServiceCollection services)
      {
          // Register core services
          services.AddSingleton<IYamlSettingsCache, YamlSettingsCache>();
          
          // Register message handlers
          services.AddSingleton<IMessageHandler, ConsoleMessageHandler>();
          
          return services;
      }
  }
  ```

## Testing Setup

### Test Projects
- [ ] Create test projects:
  ```bash
  dotnet new xunit -n Classic.Core.Tests -o tests/Classic.Core.Tests
  dotnet new xunit -n Classic.Infrastructure.Tests -o tests/Classic.Infrastructure.Tests
  dotnet sln add tests/Classic.Core.Tests/Classic.Core.Tests.csproj
  dotnet sln add tests/Classic.Infrastructure.Tests/Classic.Infrastructure.Tests.csproj
  ```

### Test Packages
- [ ] Add testing packages:
  ```bash
  dotnet add package FluentAssertions
  dotnet add package Moq
  dotnet add package Microsoft.Extensions.Logging.Abstractions
  ```

### Initial Tests
- [ ] Create `YamlSettingsCacheTests.cs`
- [ ] Create `FileUtilitiesTests.cs`
- [ ] Create `MessageHandlerTests.cs`

## Configuration Files

### Create YAML structure
- [ ] Create `CLASSIC Data` folder in solution root
- [ ] Create `databases` subfolder
- [ ] Port initial YAML files:
  - [ ] `CLASSIC Main.yaml`
  - [ ] `CLASSIC Fallout4.yaml`
  - [ ] `CLASSIC SkyrimSE.yaml`

### Global.json
- [ ] Create `global.json`:
  ```json
  {
    "sdk": {
      "version": "8.0.100",
      "rollForward": "latestMinor"
    }
  }
  ```

### .editorconfig
- [ ] Create `.editorconfig` for consistent code style

## Documentation
- [ ] Create `README.md` for the solution
- [ ] Create `ARCHITECTURE.md` documenting the design decisions
- [ ] Create `CONTRIBUTING.md` with development guidelines

## Build and Verify
- [ ] Build entire solution: `dotnet build`
- [ ] Run tests: `dotnet test`
- [ ] Verify no compiler warnings
- [ ] Check code analysis results