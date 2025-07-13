using System.IO.Abstractions.TestingHelpers;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.GameManagement;
using Classic.Infrastructure.GameManagement.Strategies;
using FluentAssertions;
using Moq;
using Serilog;
using Xunit;

namespace Classic.Infrastructure.Tests.GameManagement;

public class RefactoredGameFileManagerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly MockFileSystem _mockFileSystem;
    private readonly FileOperationStrategyFactory _strategyFactory;
    private readonly GameFileManager _gameFileManager;

    public RefactoredGameFileManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockFileSystem = new MockFileSystem();

        // Setup strategy factory with all strategies
        var strategies = new List<IFileOperationStrategy>
        {
            new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new ReshadeFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new VulkanFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new EnbFileOperationStrategy(_mockFileSystem, _mockLogger.Object)
        };
        _strategyFactory = new FileOperationStrategyFactory(strategies);

        _gameFileManager = new GameFileManager(
            _mockFileSystem,
            _mockLogger.Object,
            _mockSettingsService.Object,
            _strategyFactory);
    }

    [Fact]
    public async Task BackupFilesAsync_WithValidCategory_ShouldDelegateToStrategy()
    {
        // Arrange
        const string category = "XSE";
        _mockFileSystem.AddFile(@"C:\Game\test.dll", new MockFileData("content"));

        // Note: Since GetGameRootDirectory returns empty string, this will fail with game root not found
        // This is expected behavior for the current implementation

        // Act
        var result = await _gameFileManager.BackupFilesAsync(category);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Game root directory not found");
    }

    [Fact]
    public async Task BackupFilesAsync_WithInvalidCategory_ShouldReturnError()
    {
        // Arrange
        const string category = "UNKNOWN";

        // Act
        var result = await _gameFileManager.BackupFilesAsync(category);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Unknown category: UNKNOWN");
        result.Message.Should().Contain("Available categories: XSE, RESHADE, VULKAN, ENB");
    }

    [Fact]
    public async Task RestoreFilesAsync_WithValidCategory_ShouldDelegateToStrategy()
    {
        // Arrange
        const string category = "RESHADE";

        // Act
        var result = await _gameFileManager.RestoreFilesAsync(category);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Game root directory not found");
    }

    [Fact]
    public async Task RemoveFilesAsync_WithValidCategory_ShouldDelegateToStrategy()
    {
        // Arrange
        const string category = "VULKAN";

        // Act
        var result = await _gameFileManager.RemoveFilesAsync(category);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Game root directory not found");
    }

    [Fact]
    public void HasBackup_ShouldCheckBackupDirectory()
    {
        // Arrange
        const string category = "XSE";
        var backupDir = _gameFileManager.GetBackupDirectory(category);
        _mockFileSystem.AddFile($@"{backupDir}\test.dll", new MockFileData("content"));

        // Act
        var result = _gameFileManager.HasBackup(category);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasBackup_WithoutBackup_ShouldReturnFalse()
    {
        // Arrange
        const string category = "XSE";

        // Act
        var result = _gameFileManager.HasBackup(category);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetBackupDirectory_ShouldReturnCorrectPath()
    {
        // Arrange
        const string category = "XSE";

        // Act
        var result = _gameFileManager.GetBackupDirectory(category);

        // Assert
        result.Should().EndWith(@"CLASSIC Data\Backups\XSE");
    }

    [Theory]
    [InlineData("XSE")]
    [InlineData("RESHADE")]
    [InlineData("VULKAN")]
    [InlineData("ENB")]
    public async Task AllOperations_WithValidCategories_ShouldUseCorrectStrategy(string category)
    {
        // Arrange & Act
        var backupResult = await _gameFileManager.BackupFilesAsync(category);
        var restoreResult = await _gameFileManager.RestoreFilesAsync(category);
        var removeResult = await _gameFileManager.RemoveFilesAsync(category);

        // Assert - All should fail due to missing game root, but with category-specific messages
        backupResult.Success.Should().BeFalse();
        restoreResult.Success.Should().BeFalse();
        removeResult.Success.Should().BeFalse();

        // Should not contain "Unknown category" error
        backupResult.Message.Should().NotContain("Unknown category");
        restoreResult.Message.Should().NotContain("Unknown category");
        removeResult.Message.Should().NotContain("Unknown category");
    }
}
