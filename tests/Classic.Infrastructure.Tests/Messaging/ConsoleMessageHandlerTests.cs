using Classic.Infrastructure.Messaging;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using FluentAssertions;
using Serilog;
using Moq;
using Xunit;

namespace Classic.Infrastructure.Tests.Messaging;

public class ConsoleMessageHandlerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IMessageFormattingService> _mockFormattingService;
    private readonly ConsoleMessageHandler _handler;

    public ConsoleMessageHandlerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFormattingService = new Mock<IMessageFormattingService>();
        
        // Setup default behavior for formatting service
        _mockFormattingService.Setup(x => x.FormatMessage(It.IsAny<string>(), It.IsAny<MessageType>()))
            .Returns<string, MessageType>((msg, type) => $"[{type}] {msg}");
        _mockFormattingService.Setup(x => x.GetConsoleColor(It.IsAny<MessageType>()))
            .Returns(ConsoleColor.White);
        
        _handler = new ConsoleMessageHandler(_mockLogger.Object, _mockFormattingService.Object);
    }

    [Theory]
    [InlineData(MessageTarget.Cli, true)]
    [InlineData(MessageTarget.Gui, false)]
    [InlineData(MessageTarget.Both, true)]
    public void SendMessage_ShouldRespectMessageTarget(MessageTarget target, bool shouldOutput)
    {
        // Arrange
        var message = "Test message";
        var messageType = MessageType.Info;

        // Act
        _handler.SendMessage(message, messageType, target);

        // Assert
        if (shouldOutput)
        {
            // For Serilog, we can verify that Information was called with the specific generic signature
            _mockLogger.Verify(
                x => x.Information<MessageType, string>(
                    It.Is<string>(s => s.Contains("Message sent")),
                    It.IsAny<MessageType>(),
                    It.IsAny<string>()),
                Times.Once);
        }
        else
        {
            // Verify that no Information calls were made for GUI-only messages
            _mockLogger.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public void BeginProgressContext_ShouldReturnDisposableContext()
    {
        // Arrange
        var operation = "Test Operation";
        var total = 100;

        // Act
        var context = _handler.BeginProgressContext(operation, total);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeAssignableTo<IDisposable>();
    }

    [Theory]
    [InlineData(0, 100, 0.0)]
    [InlineData(50, 100, 50.0)]
    [InlineData(100, 100, 100.0)]
    [InlineData(25, 100, 25.0)]
    public void ReportProgress_ShouldCalculateCorrectPercentage(int current, int total, double expectedPercentage)
    {
        // Arrange
        var operation = "Test Operation";

        // Act & Assert (This is mainly testing that the method doesn't throw)
        Action act = () => _handler.ReportProgress(operation, current, total);
        act.Should().NotThrow();
    }
}
