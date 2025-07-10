# Python to C# Migration Reference

## Common Type Conversions

| Python | C# | Notes |
|--------|-----|-------|
| `list[str]` | `List<string>` or `IList<string>` | Use interface for parameters |
| `dict[str, Any]` | `Dictionary<string, object>` | Consider strongly-typed alternatives |
| `tuple[str, int, bool]` | `(string, int, bool)` or custom class | ValueTuple for simple cases |
| `set[str]` | `HashSet<string>` | For unique collections |
| `Path` | `string` or `FileInfo` | Use `Path.Combine()` for joining |
| `None` | `null` | Consider nullable reference types |
| `Optional[str]` | `string?` | C# 8+ nullable references |

## String Operations

**Python:**
```python
# String formatting
msg = f"Processing {filename} - {count}/{total}"

# Multi-line strings
text = """Line 1
Line 2
Line 3"""

# String joining
result = "\n".join(lines)

# String splitting
parts = line.split(" | ", 1)

# String replacement
fixed = text.replace("|", "\n", 1)
```

**C#:**
```csharp
// String interpolation
var msg = $"Processing {filename} - {count}/{total}";

// Multi-line strings (C# 11+)
var text = """
    Line 1
    Line 2
    Line 3
    """;

// String joining
var result = string.Join("\n", lines);

// String splitting
var parts = line.Split(" | ", 2, StringSplitOptions.None);

// String replacement (first occurrence only)
var index = text.IndexOf("|");
var fixed = index >= 0 
    ? text.Substring(0, index) + "\n" + text.Substring(index + 1)
    : text;
```

## File Operations

**Python:**
```python
# Read file
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Read lines
lines = filepath.read_text().splitlines()

# Write file
filepath.write_text(content, encoding='utf-8')

# Check existence
if filepath.exists() and filepath.is_file():
    # process
```

**C#:**
```csharp
// Read file
var content = await File.ReadAllTextAsync(filepath, Encoding.UTF8);

// Read lines
var lines = await File.ReadAllLinesAsync(filepath);

// Write file
await File.WriteAllTextAsync(filepath, content, Encoding.UTF8);

// Check existence
if (File.Exists(filepath))
{
    // process
}
```

## Collections and LINQ

**Python:**
```python
# List comprehension
filtered = [x for x in items if x.startswith("test")]

# Dictionary comprehension
mapping = {k: v for k, v in pairs if v is not None}

# Any/All
if any(item.is_valid for item in items):
    # process

# Enumerate
for index, value in enumerate(items):
    print(f"{index}: {value}")
```

**C#:**
```csharp
// LINQ filtering
var filtered = items.Where(x => x.StartsWith("test")).ToList();

// Dictionary creation
var mapping = pairs
    .Where(kvp => kvp.Value != null)
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// Any/All
if (items.Any(item => item.IsValid))
{
    // process
}

// Enumerate with index
foreach (var (value, index) in items.Select((v, i) => (v, i)))
{
    Console.WriteLine($"{index}: {value}");
}
```

## Exception Handling

**Python:**
```python
try:
    result = process_file(path)
except FileNotFoundError:
    logger.error(f"File not found: {path}")
    return None
except Exception as e:
    logger.error(f"Unexpected error: {e}")
    raise
finally:
    cleanup()
```

**C#:**
```csharp
try
{
    var result = ProcessFile(path);
    return result;
}
catch (FileNotFoundException)
{
    _logger.LogError("File not found: {Path}", path);
    return null;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    throw;
}
finally
{
    Cleanup();
}
```

## Async Patterns

**Python:**
```python
# Async function
async def process_async(files: list[Path]) -> list[Result]:
    tasks = [process_file_async(f) for f in files]
    return await asyncio.gather(*tasks)

# Async context manager
async with aiofiles.open(path, 'r') as f:
    content = await f.read()

# Async iteration
async for item in fetch_items_async():
    await process_item(item)
```

**C#:**
```csharp
// Async function
public async Task<List<Result>> ProcessAsync(IEnumerable<string> files)
{
    var tasks = files.Select(f => ProcessFileAsync(f));
    return await Task.WhenAll(tasks);
}

// Using statement (async disposal)
await using var stream = File.OpenRead(path);
var content = await ReadStreamAsync(stream);

// Async iteration
await foreach (var item in FetchItemsAsync())
{
    await ProcessItemAsync(item);
}
```

## Class Definitions

**Python:**
```python
@dataclass
class ScanResult:
    filename: str
    errors: list[str] = field(default_factory=list)
    timestamp: datetime = field(default_factory=datetime.now)
    
    def add_error(self, error: str) -> None:
        self.errors.append(error)
```

**C#:**
```csharp
public class ScanResult
{
    public string Filename { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}

// Or as a record (C# 9+)
public record ScanResult(
    string Filename,
    List<string> Errors = null,
    DateTime? Timestamp = null)
{
    public List<string> Errors { get; init; } = Errors ?? new();
    public DateTime Timestamp { get; init; } = Timestamp ?? DateTime.Now;
}
```

## Pattern Matching

**Python:**
```python
# Match statement (Python 3.10+)
match error_type:
    case "warning":
        level = LogLevel.Warning
    case "error" | "critical":
        level = LogLevel.Error
    case _:
        level = LogLevel.Info
```

**C#:**
```csharp
// Switch expression
var level = error_type switch
{
    "warning" => LogLevel.Warning,
    "error" or "critical" => LogLevel.Error,
    _ => LogLevel.Info
};

// Pattern matching with conditions
var message = result switch
{
    { IsSuccess: true, Value: var v } => $"Success: {v}",
    { Error: var e } when e != null => $"Error: {e}",
    _ => "Unknown result"
};
```

## Regular Expressions

**Python:**
```python
import regex as re

pattern = re.compile(r"\[(FE:([0-9A-F]{3})|[0-9A-F]{2})\]", re.IGNORECASE)
matches = pattern.findall(text)

# Search with groups
if match := pattern.search(text):
    full_match = match.group(0)
    first_group = match.group(1)
```

**C#:**
```csharp
using System.Text.RegularExpressions;

var pattern = new Regex(@"\[(FE:([0-9A-F]{3})|[0-9A-F]{2})\]", 
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
var matches = pattern.Matches(text);

// Search with groups
var match = pattern.Match(text);
if (match.Success)
{
    var fullMatch = match.Value;
    var firstGroup = match.Groups[1].Value;
}
```

## Working with JSON/YAML

**Python:**
```python
# JSON
import json
data = json.loads(json_string)
json_string = json.dumps(data, indent=2)

# YAML
import yaml
with open('config.yaml') as f:
    config = yaml.safe_load(f)
```

**C#:**
```csharp
// JSON (using System.Text.Json)
var data = JsonSerializer.Deserialize<ConfigData>(jsonString);
var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});

// YAML (using YamlDotNet)
var deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .Build();
var config = deserializer.Deserialize<ConfigData>(yamlText);
```

## Logging

**Python:**
```python
import logging

logger = logging.getLogger(__name__)
logger.info(f"Processing {filename}")
logger.error(f"Failed to process: {error}", exc_info=True)
```

**C#:**
```csharp
using Microsoft.Extensions.Logging;

public class Scanner
{
    private readonly ILogger<Scanner> _logger;
    
    public Scanner(ILogger<Scanner> logger)
    {
        _logger = logger;
    }
    
    public void Process()
    {
        _logger.LogInformation("Processing {Filename}", filename);
        _logger.LogError(exception, "Failed to process");
    }
}
```

## Common Gotchas

### 1. String Comparison
**Python:** Case-sensitive by default
```python
if name.lower() == "test":
```

**C#:** Use StringComparison
```csharp
if (name.Equals("test", StringComparison.OrdinalIgnoreCase))
```

### 2. Division
**Python:** Float division by default in Python 3
```python
result = 5 / 2  # 2.5
```

**C#:** Integer division for integers
```csharp
var result = 5 / 2;     // 2
var result = 5.0 / 2;   // 2.5
```

### 3. Null/None Handling
**Python:**
```python
value = config.get('key', 'default')
```

**C#:**
```csharp
var value = config.GetValueOrDefault("key", "default");
// or
var value = config.TryGetValue("key", out var v) ? v : "default";
```

### 4. List Slicing
**Python:**
```python
subset = items[1:5]
last_three = items[-3:]
```

**C#:**
```csharp
var subset = items.Skip(1).Take(4).ToList();
var lastThree = items.TakeLast(3).ToList();
```