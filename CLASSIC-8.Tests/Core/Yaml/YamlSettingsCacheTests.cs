using CLASSIC_8.Core;
using CLASSIC_8.Core.Yaml;
using Xunit;

namespace CLASSIC_8.Tests.Core.Yaml;

public class YamlSettingsCacheTests : IDisposable
{
    private readonly YamlSettingsCache _cache;
    private readonly GameManager _gameManager;
    private readonly string _testYamlPath = "test_settings.yaml";
    
    public YamlSettingsCacheTests()
    {
        _gameManager = new GameManager();
        _cache = new YamlSettingsCache(_gameManager);
        
        // Create a test YAML file
        var testContent = @"
test_settings:
  string_value: ""test string""
  int_value: 42
  bool_value: true
  nested:
    value: ""nested value""
  list_value:
    - item1
    - item2
    - item3
";
        File.WriteAllText(_testYamlPath, testContent);
    }
    
    public void Dispose()
    {
        if (File.Exists(_testYamlPath))
        {
            File.Delete(_testYamlPath);
        }
    }
    
    [Fact]
    public void GetPathForStore_ReturnsCorrectPaths()
    {
        // Act & Assert
        Assert.Equal("CLASSIC Data/databases/CLASSIC Main.yaml", _cache.GetPathForStore(YamlStore.Main));
        Assert.Equal("CLASSIC Settings.yaml", _cache.GetPathForStore(YamlStore.Settings));
        Assert.Equal("CLASSIC Ignore.yaml", _cache.GetPathForStore(YamlStore.Ignore));
        Assert.Equal("CLASSIC Data/databases/CLASSIC Fallout4.yaml", _cache.GetPathForStore(YamlStore.Game));
        Assert.Equal("CLASSIC Data/CLASSIC Fallout4 Local.yaml", _cache.GetPathForStore(YamlStore.GameLocal));
    }
    
    [Fact]
    public void GetPathForStore_UsesCurrentGame()
    {
        // Arrange
        _gameManager.CurrentGame = "Skyrim";
        
        // Act
        var gamePath = _cache.GetPathForStore(YamlStore.Game);
        var gameLocalPath = _cache.GetPathForStore(YamlStore.GameLocal);
        
        // Assert
        Assert.Equal("CLASSIC Data/databases/CLASSIC Skyrim.yaml", gamePath);
        Assert.Equal("CLASSIC Data/CLASSIC Skyrim Local.yaml", gameLocalPath);
    }
    
    [Fact]
    public void GetSetting_ReadsStringValue()
    {
        // Act
        var result = _cache.GetSetting<string>(YamlStore.Test, "test_settings.string_value");
        
        // Assert
        Assert.Equal("test string", result);
    }
    
    [Fact]
    public void GetSetting_ReadsIntValue()
    {
        // Act
        var result = _cache.GetSetting<int>(YamlStore.Test, "test_settings.int_value");
        
        // Assert
        Assert.Equal(42, result);
    }
    
    [Fact]
    public void GetSetting_ReadsBoolValue()
    {
        // Act
        var result = _cache.GetSetting<bool>(YamlStore.Test, "test_settings.bool_value");
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void GetSetting_ReadsNestedValue()
    {
        // Act
        var result = _cache.GetSetting<string>(YamlStore.Test, "test_settings.nested.value");
        
        // Assert
        Assert.Equal("nested value", result);
    }
    
    [Fact]
    public void GetSetting_ReadsListValue()
    {
        // Act
        var result = _cache.GetSetting<List<string>>(YamlStore.Test, "test_settings.list_value");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("item1", result[0]);
        Assert.Equal("item2", result[1]);
        Assert.Equal("item3", result[2]);
    }
    
    [Fact]
    public void GetSetting_ReturnsNullForMissingKey()
    {
        // Act
        var result = _cache.GetSetting<string>(YamlStore.Test, "test_settings.missing_key");
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void GetSetting_WritesValue()
    {
        // Act
        var newValue = "updated value";
        var result = _cache.GetSetting<string>(YamlStore.Test, "test_settings.new_value", newValue);
        
        // Assert
        Assert.Equal(newValue, result);
        
        // Verify it was persisted
        var readBack = _cache.GetSetting<string>(YamlStore.Test, "test_settings.new_value");
        Assert.Equal(newValue, readBack);
    }
    
    [Fact]
    public void GetSetting_ThrowsWhenModifyingStaticStore()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _cache.GetSetting<string>(YamlStore.Main, "some.key", "new value"));
    }
    
    [Fact]
    public void ReloadYamlFile_ClearsCache()
    {
        // Arrange
        var initialValue = _cache.GetSetting<string>(YamlStore.Test, "test_settings.string_value");
        
        // Modify file directly
        var content = File.ReadAllText(_testYamlPath);
        content = content.Replace("test string", "modified string");
        File.WriteAllText(_testYamlPath, content);
        
        // Act
        _cache.ReloadYamlFile(YamlStore.Test);
        var newValue = _cache.GetSetting<string>(YamlStore.Test, "test_settings.string_value");
        
        // Assert
        Assert.Equal("test string", initialValue);
        Assert.Equal("modified string", newValue);
    }
}