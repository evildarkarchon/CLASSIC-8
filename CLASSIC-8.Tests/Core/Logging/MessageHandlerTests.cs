using CLASSIC_8.Core.Logging;
using Xunit;

namespace CLASSIC_8.Tests.Core.Logging;

public class MessageHandlerTests : IDisposable
{
    private readonly MessageHandler _handler;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;
    
    public MessageHandlerTests()
    {
        _handler = new MessageHandler(null, false); // CLI mode
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
    public void IsGuiMode_ReturnsFalseForCliMode()
    {
        // Act & Assert
        Assert.False(_handler.IsGuiMode);
    }
    
    [Fact]
    public void IsGuiMode_ReturnsTrueForGuiMode()
    {
        // Arrange
        var guiHandler = new MessageHandler(null, true);
        
        // Act & Assert
        Assert.True(guiHandler.IsGuiMode);
    }
    
    [Fact]
    public void Info_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test info message";
        
        // Act
        _handler.Info(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[INFO]", output);
        Assert.Contains(message, output);
    }
    
    [Fact]
    public void Warning_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test warning message";
        
        // Act
        _handler.Warning(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[WARNING]", output);
        Assert.Contains(message, output);
    }
    
    [Fact]
    public void Error_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test error message";
        
        // Act
        _handler.Error(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[ERROR]", output);
        Assert.Contains(message, output);
    }
    
    [Fact]
    public void Status_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test status message";
        
        // Act
        _handler.Status(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[STATUS]", output);
        Assert.Contains(message, output);
    }
    
    [Fact]
    public void Notice_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test notice message";
        
        // Act
        _handler.Notice(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[NOTICE]", output);
        Assert.Contains(message, output);
    }
    
    [Fact]
    public void Complete_WritesToConsoleInCliMode()
    {
        // Arrange
        const string message = "Test completion message";
        
        // Act
        _handler.Complete(message);
        
        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("[COMPLETE]", output);
        Assert.Contains(message, output);
    }
}