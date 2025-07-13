using Classic.Core.Enums;
using Classic.Infrastructure.Messaging;
using FluentAssertions;
using Xunit;

namespace Classic.Infrastructure.Tests.Messaging;

public class MessageFormattingServiceTests
{
    private readonly MessageFormattingService _service = new();

    [Theory]
    [InlineData(MessageType.Error, "[ERROR]")]
    [InlineData(MessageType.Warning, "[WARNING]")]
    [InlineData(MessageType.Success, "[SUCCESS]")]
    [InlineData(MessageType.Info, "[INFO]")]
    [InlineData(MessageType.Debug, "[DEBUG]")]
    [InlineData(MessageType.Critical, "[CRITICAL]")]
    public void GetMessagePrefix_ShouldReturnCorrectPrefix(MessageType type, string expectedPrefix)
    {
        // Act
        var result = _service.GetMessagePrefix(type);

        // Assert
        result.Should().Be(expectedPrefix);
    }

    [Theory]
    [InlineData(MessageType.Error, ConsoleColor.Red)]
    [InlineData(MessageType.Critical, ConsoleColor.DarkRed)]
    [InlineData(MessageType.Warning, ConsoleColor.Yellow)]
    [InlineData(MessageType.Success, ConsoleColor.Green)]
    [InlineData(MessageType.Debug, ConsoleColor.Gray)]
    [InlineData(MessageType.Info, ConsoleColor.White)]
    public void GetConsoleColor_ShouldReturnCorrectColor(MessageType type, ConsoleColor expectedColor)
    {
        // Act
        var result = _service.GetConsoleColor(type);

        // Assert
        result.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData("Test message", MessageType.Error, "[ERROR] Test message")]
    [InlineData("Warning occurred", MessageType.Warning, "[WARNING] Warning occurred")]
    [InlineData("Success!", MessageType.Success, "[SUCCESS] Success!")]
    public void FormatMessage_ShouldFormatMessageWithPrefix(string message, MessageType type, string expected)
    {
        // Act
        var result = _service.FormatMessage(message, type);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(MessageType.Error, "❌")]
    [InlineData(MessageType.Critical, "🚨")]
    [InlineData(MessageType.Warning, "⚠️")]
    [InlineData(MessageType.Success, "✅")]
    [InlineData(MessageType.Debug, "🔍")]
    [InlineData(MessageType.Info, "ℹ️")]
    public void GetMessageIcon_ShouldReturnCorrectIcon(MessageType type, string expectedIcon)
    {
        // Act
        var result = _service.GetMessageIcon(type);

        // Assert
        result.Should().Be(expectedIcon);
    }
}
