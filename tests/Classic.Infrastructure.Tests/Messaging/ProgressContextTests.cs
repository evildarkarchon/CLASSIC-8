using Classic.Core.Interfaces;
using Classic.Infrastructure.Messaging;
using FluentAssertions;
using Moq;
using Xunit;

namespace Classic.Infrastructure.Tests.Messaging;

public class ProgressContextTests
{
    private readonly Mock<IMessageHandler> _mockHandler = new();
    private const string TestOperation = "Test Operation";
    private const int TestTotal = 100;

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Act
        using var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);

        // Assert
        context.Operation.Should().Be(TestOperation);
        context.Total.Should().Be(TestTotal);
        context.Current.Should().Be(0);
        context.Percentage.Should().Be(0);
        context.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldReportInitialProgress()
    {
        // Act
        using var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);

        // Assert
        _mockHandler.Verify(h => h.ReportProgress(TestOperation, 0, TestTotal), Times.Once);
    }

    [Fact]
    public void Increment_ShouldUpdateCurrentAndReportProgress()
    {
        // Arrange
        using var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);
        _mockHandler.Reset(); // Clear initial progress call

        // Act
        context.Increment();

        // Assert
        context.Current.Should().Be(1);
        _mockHandler.Verify(h => h.ReportProgress(TestOperation, 1, TestTotal), Times.Once);
    }

    [Fact]
    public void SetProgress_ShouldUpdateCurrentAndReportProgress()
    {
        // Arrange
        using var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);
        _mockHandler.Reset(); // Clear initial progress call

        // Act
        context.SetProgress(50);

        // Assert
        context.Current.Should().Be(50);
        context.Percentage.Should().Be(50);
        _mockHandler.Verify(h => h.ReportProgress(TestOperation, 50, TestTotal), Times.Once);
    }

    [Fact]
    public void Complete_ShouldSetProgressToTotal()
    {
        // Arrange
        using var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);
        _mockHandler.Reset(); // Clear initial progress call

        // Act
        context.Complete();

        // Assert
        context.Current.Should().Be(TestTotal);
        context.IsCompleted.Should().BeTrue();
        _mockHandler.Verify(h => h.ReportProgress(TestOperation, TestTotal, TestTotal), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCompleteProgressIfNotAlreadyCompleted()
    {
        // Arrange
        var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);
        context.SetProgress(50);
        _mockHandler.Reset(); // Clear previous calls

        // Act
        context.Dispose();

        // Assert
        _mockHandler.Verify(h => h.ReportProgress(TestOperation, TestTotal, TestTotal), Times.Once);
    }

    [Fact]
    public void AfterDispose_MethodsShouldThrowObjectDisposedException()
    {
        // Arrange
        var context = new ProgressContext(_mockHandler.Object, TestOperation, TestTotal);
        context.Dispose();

        // Act & Assert
        context.Invoking(c => c.Increment()).Should().Throw<ObjectDisposedException>();
        context.Invoking(c => c.SetProgress(50)).Should().Throw<ObjectDisposedException>();
        context.Invoking(c => c.Complete()).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new ProgressContext(null!, TestOperation, TestTotal);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void Constructor_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new ProgressContext(_mockHandler.Object, null!, TestTotal);
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }
}
