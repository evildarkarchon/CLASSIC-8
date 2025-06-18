using System.Reflection;
using CLASSIC_8.Core;
using CLASSIC_8.Core.Yaml;
using Xunit;

namespace CLASSIC_8.Tests.Core.Yaml;

public class YamlSettingsHelperTests : IDisposable
{
    private readonly YamlSettingsCache _cache;
    private readonly GameManager _gameManager;
    private readonly string _testSettingsPath = "CLASSIC Settings.yaml";

    public YamlSettingsHelperTests()
    {
        _gameManager = new GameManager();
        _cache = new YamlSettingsCache(_gameManager);
        YamlSettingsHelper.Initialize(_cache);

        // Clean up any existing settings file
        if (File.Exists(_testSettingsPath)) File.Delete(_testSettingsPath);
    }

    public void Dispose()
    {
        if (File.Exists(_testSettingsPath)) File.Delete(_testSettingsPath);
    }

    [Fact]
    public void Initialize_ThrowsWhenNotInitialized()
    {
        // Arrange - Create a new helper class to test uninitialized state
        var helperType = typeof(YamlSettingsHelper);
        var cacheField = helperType.GetField("_yamlCache", BindingFlags.NonPublic | BindingFlags.Static);
        var originalValue = cacheField?.GetValue(null);
        cacheField?.SetValue(null, null);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                YamlSettingsHelper.YamlSettings<string>(YamlStore.Settings, "test"));
        }
        finally
        {
            // Restore original value
            cacheField?.SetValue(null, originalValue);
        }
    }

    [Fact]
    public void ClassicSettings_CreatesSettingsFileIfNotExists()
    {
        // Arrange
        Assert.False(File.Exists(_testSettingsPath));

        // Create a mock Main.yaml with default settings template
        var mainYamlPath = "CLASSIC Data/databases/CLASSIC Main.yaml";
        Directory.CreateDirectory(Path.GetDirectoryName(mainYamlPath)!);

        var mainContent = @"
CLASSIC_Info:
  default_settings: |
    CLASSIC_Settings:
      Test_Setting: ""default value""
      Another_Setting: 123
";
        File.WriteAllText(mainYamlPath, mainContent);

        try
        {
            // Act
            var result = YamlSettingsHelper.ClassicSettings<string>("Test_Setting");

            // Assert
            Assert.True(File.Exists(_testSettingsPath));
            Assert.Equal("default value", result);

            // Verify the settings file was created with the correct content
            var createdContent = File.ReadAllText(_testSettingsPath);
            Assert.Contains("Test_Setting: \"default value\"", createdContent);
            Assert.Contains("Another_Setting: 123", createdContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(mainYamlPath)) File.Delete(mainYamlPath);
            if (Directory.Exists("CLASSIC Data")) Directory.Delete("CLASSIC Data", true);
        }
    }

    [Fact]
    public void YamlSettings_HandlesFileInfoConversion()
    {
        // Arrange
        var testPath = "tests/test_settings.yaml";
        var testContent = @"
test:
  file_path: ""C:/test/file.txt""
";
        Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);
        File.WriteAllText(testPath, testContent);

        try
        {
            // Act
            var result = YamlSettingsHelper.YamlSettings<FileInfo>(YamlStore.Test, "test.file_path");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("C:/test/file.txt", result.FullName);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testPath)) File.Delete(testPath);
            if (Directory.Exists("tests")) Directory.Delete("tests", true);
        }
    }

    [Fact]
    public void YamlSettings_HandlesDirectoryInfoConversion()
    {
        // Arrange
        var testPath = "tests/test_settings.yaml";
        var testContent = @"
test:
  dir_path: ""C:/test/directory""
";
        Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);
        File.WriteAllText(testPath, testContent);

        try
        {
            // Act
            var result = YamlSettingsHelper.YamlSettings<DirectoryInfo>(YamlStore.Test, "test.dir_path");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("C:/test/directory", result.FullName);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testPath)) File.Delete(testPath);
            if (Directory.Exists("tests")) Directory.Delete("tests", true);
        }
    }
}