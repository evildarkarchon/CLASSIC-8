using System.Reflection;
using CLASSIC_8.Core.Logging;
using Xunit;

namespace CLASSIC_8.Tests.Core.Logging;

public class MessageHandlerExtensionsTests : IDisposable
{
    private readonly StringWriter _consoleOutput;
    private readonly MessageHandler _handler;
    private readonly TextWriter _originalOutput;

    public MessageHandlerExtensionsTests()
    {
        _handler = new MessageHandler();
        MessageHandlerExtensions.InitializeMessageHandler(_handler);

        _consoleOutput = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void InitializeMessageHandler_SetsGlobalInstance()
    {
        // Arrange
        var newHandler = new MessageHandler(null, true);

        // Act
        MessageHandlerExtensions.InitializeMessageHandler(newHandler);

        // Assert
        Assert.Same(newHandler, MessageHandlerExtensions.MessageHandler);

        // Reset for other tests
        MessageHandlerExtensions.InitializeMessageHandler(_handler);
    }

    [Fact]
    public void MessageHandler_ThrowsWhenNotInitialized()
    {
        // Arrange - Reset to uninitialized state
        var handlerType = typeof(MessageHandlerExtensions);
        var handlerField = handlerType.GetField("_messageHandler", BindingFlags.NonPublic | BindingFlags.Static);
        var originalValue = handlerField?.GetValue(null);
        handlerField?.SetValue(null, null);

        try
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => MessageHandlerExtensions.MessageHandler);
        }
        finally
        {
            // Restore
            handlerField?.SetValue(null, originalValue);
        }
    }

    [Fact]
    public void MsgInfo_CallsHandlerInfo()
    {
        // Arrange
        const string message = "Test info message";

        // Act
        MessageHandlerExtensions.MsgInfo(message);

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[INFO]", output);
        Assert.Contains(message, output);
    }

    [Fact]
    public void MsgWarning_CallsHandlerWarning()
    {
        // Arrange
        const string message = "Test warning message";

        // Act
        MessageHandlerExtensions.MsgWarning(message);

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[WARNING]", output);
        Assert.Contains(message, output);
    }

    [Fact]
    public void MsgError_CallsHandlerError()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        MessageHandlerExtensions.MsgError(message);

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[ERROR]", output);
        Assert.Contains(message, output);
    }

    [Fact]
    public void MsgStatus_CallsHandlerStatus()
    {
        // Arrange
        const string message = "Test status message";

        // Act
        MessageHandlerExtensions.MsgStatus(message);

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[STATUS]", output);
        Assert.Contains(message, output);
    }
}