using Classic.Core.Interfaces;
using Classic.Infrastructure.GameManagement.Strategies;
using FluentAssertions;
using Moq;
using Serilog;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Classic.Infrastructure.Tests.GameManagement.Strategies;

public class FileOperationStrategyFactoryTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly MockFileSystem _mockFileSystem;

    public FileOperationStrategyFactoryTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFileSystem = new MockFileSystem();
    }

    [Fact]
    public void Constructor_ShouldRegisterAllProvidedStrategies()
    {
        // Arrange
        var strategies = new List<IFileOperationStrategy>
        {
            new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new ReshadeFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new VulkanFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new EnbFileOperationStrategy(_mockFileSystem, _mockLogger.Object)
        };

        // Act
        var factory = new FileOperationStrategyFactory(strategies);

        // Assert
        factory.GetAvailableCategories().Should().HaveCount(4);
        factory.GetAvailableCategories().Should().Contain(["XSE", "RESHADE", "VULKAN", "ENB"]);
    }

    [Fact]
    public void GetStrategy_WithValidCategory_ShouldReturnCorrectStrategy()
    {
        // Arrange
        var strategies = new List<IFileOperationStrategy>
        {
            new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object),
            new ReshadeFileOperationStrategy(_mockFileSystem, _mockLogger.Object)
        };
        var factory = new FileOperationStrategyFactory(strategies);

        // Act
        var xseStrategy = factory.GetStrategy("XSE");
        var reshadeStrategy = factory.GetStrategy("RESHADE");

        // Assert
        xseStrategy.Should().NotBeNull();
        xseStrategy!.Category.Should().Be("XSE");

        reshadeStrategy.Should().NotBeNull();
        reshadeStrategy!.Category.Should().Be("RESHADE");
    }

    [Fact]
    public void GetStrategy_WithInvalidCategory_ShouldReturnNull()
    {
        // Arrange
        var strategies = new List<IFileOperationStrategy>
        {
            new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object)
        };
        var factory = new FileOperationStrategyFactory(strategies);

        // Act
        var result = factory.GetStrategy("UNKNOWN");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetStrategy_WithCaseInsensitiveCategory_ShouldReturnCorrectStrategy()
    {
        // Arrange
        var strategies = new List<IFileOperationStrategy>
        {
            new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object)
        };
        var factory = new FileOperationStrategyFactory(strategies);

        // Act
        var result1 = factory.GetStrategy("xse");
        var result2 = factory.GetStrategy("XSE");
        var result3 = factory.GetStrategy("Xse");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();

        result1!.Category.Should().Be("XSE");
        result2!.Category.Should().Be("XSE");
        result3!.Category.Should().Be("XSE");
    }

    [Fact]
    public void RegisterStrategy_ShouldAddNewStrategy()
    {
        // Arrange
        var factory = new FileOperationStrategyFactory([]);
        var strategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);

        // Act
        factory.RegisterStrategy(strategy);

        // Assert
        factory.GetAvailableCategories().Should().Contain("XSE");
        factory.GetStrategy("XSE").Should().NotBeNull();
    }

    [Fact]
    public void RegisterStrategy_WithNullStrategy_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new FileOperationStrategyFactory([]);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.RegisterStrategy(null!));
    }

    [Fact]
    public void RegisterStrategy_WithDuplicateCategory_ShouldReplaceExistingStrategy()
    {
        // Arrange
        var originalStrategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);
        var newStrategy = new XseFileOperationStrategy(_mockFileSystem, _mockLogger.Object);
        var factory = new FileOperationStrategyFactory([originalStrategy]);

        // Act
        factory.RegisterStrategy(newStrategy);

        // Assert
        factory.GetAvailableCategories().Should().HaveCount(1);
        factory.GetStrategy("XSE").Should().Be(newStrategy);
    }
}
