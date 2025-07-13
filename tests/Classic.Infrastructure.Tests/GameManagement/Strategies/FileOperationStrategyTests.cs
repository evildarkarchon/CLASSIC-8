using System.IO.Abstractions.TestingHelpers;
using Classic.Core.Enums;
using Classic.Infrastructure.GameManagement.Strategies;
using FluentAssertions;
using Moq;
using Serilog;
using Xunit;

namespace Classic.Infrastructure.Tests.GameManagement.Strategies;

public class FileOperationStrategyTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly MockFileSystem _mockFileSystem;

    public FileOperationStrategyTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFileSystem = new MockFileSystem();
    }

    [Fact]
    public void XseStrategy_ShouldHaveCorrectCategoryAndPatterns()
    {
        // Arrange & Act
        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Assert
        strategy.Category.Should().Be("XSE");
        strategy.FilePatterns.Should().Contain(["*.dll", "*.exe", "*.log", "f4se_*", "skse64_*", "sksevr_*"]);
    }

    [Fact]
    public void ReshadeStrategy_ShouldHaveCorrectCategoryAndPatterns()
    {
        // Arrange & Act
        var strategy = new ReshadeFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Assert
        strategy.Category.Should().Be("RESHADE");
        strategy.FilePatterns.Should().Contain([
            "dxgi.dll", "d3d11.dll", "d3d9.dll", "opengl32.dll",
            "reshade.ini", "ReShade.ini"
        ]);
    }

    [Fact]
    public void VulkanStrategy_ShouldHaveCorrectCategoryAndPatterns()
    {
        // Arrange & Act
        var strategy = new VulkanFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Assert
        strategy.Category.Should().Be("VULKAN");
        strategy.FilePatterns.Should().Contain(["vulkan-1.dll", "vulkan*.dll"]);
    }

    [Fact]
    public void EnbStrategy_ShouldHaveCorrectCategoryAndPatterns()
    {
        // Arrange & Act
        var strategy = new EnbFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Assert
        strategy.Category.Should().Be("ENB");
        strategy.FilePatterns.Should().Contain([
            "d3d11.dll", "d3d9.dll",
            "enbseries.ini", "enblocal.ini",
            "enbseries/*", "enbcache/*"
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_WithBackupOperation_ShouldBackupFiles()
    {
        // Arrange
        var gameRoot = @"C:\Game";
        var backupDir = @"C:\Backup\XSE";

        _mockFileSystem.AddFile(@"C:\Game\test.dll", new MockFileData("test content"));
        _mockFileSystem.AddFile(@"C:\Game\f4se_loader.exe", new MockFileData("loader content"));

        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Act
        var result = await strategy.ExecuteAsync(GameFileOperation.Backup, gameRoot, backupDir, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedFiles.Should().HaveCount(2);
        result.ProcessedFiles.Should().Contain(["test.dll", "f4se_loader.exe"]);

        _mockFileSystem.File.Exists(@"C:\Backup\XSE\test.dll").Should().BeTrue();
        _mockFileSystem.File.Exists(@"C:\Backup\XSE\f4se_loader.exe").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithRemoveOperation_ShouldRemoveFiles()
    {
        // Arrange
        var gameRoot = @"C:\Game";
        var backupDir = @"C:\Backup\XSE"; // Not used for remove operation

        _mockFileSystem.AddFile(@"C:\Game\test.dll", new MockFileData("test content"));
        _mockFileSystem.AddFile(@"C:\Game\f4se_loader.exe", new MockFileData("loader content"));

        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Act
        var result = await strategy.ExecuteAsync(GameFileOperation.Remove, gameRoot, backupDir, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedFiles.Should().HaveCount(2);
        result.ProcessedFiles.Should().Contain(["test.dll", "f4se_loader.exe"]);

        _mockFileSystem.File.Exists(@"C:\Game\test.dll").Should().BeFalse();
        _mockFileSystem.File.Exists(@"C:\Game\f4se_loader.exe").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithRestoreOperation_ShouldRestoreFiles()
    {
        // Arrange
        var gameRoot = @"C:\Game";
        var backupDir = @"C:\Backup\XSE";

        _mockFileSystem.AddFile(@"C:\Backup\XSE\test.dll", new MockFileData("test content"));
        _mockFileSystem.AddFile(@"C:\Backup\XSE\f4se_loader.exe", new MockFileData("loader content"));

        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Act
        var result = await strategy.ExecuteAsync(GameFileOperation.Restore, gameRoot, backupDir, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedFiles.Should().HaveCount(2);
        result.ProcessedFiles.Should().Contain(["test.dll", "f4se_loader.exe"]);

        _mockFileSystem.File.Exists(@"C:\Game\test.dll").Should().BeTrue();
        _mockFileSystem.File.Exists(@"C:\Game\f4se_loader.exe").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMatchingFiles_ShouldReturnFailure()
    {
        // Arrange
        var gameRoot = @"C:\Game";
        var backupDir = @"C:\Backup\XSE";

        _mockFileSystem.AddFile(@"C:\Game\somefile.txt", new MockFileData("content"));

        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Act
        var result = await strategy.ExecuteAsync(GameFileOperation.Backup, gameRoot, backupDir, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No XSE files found");
    }
}
