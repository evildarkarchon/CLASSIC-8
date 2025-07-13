using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Configuration;
using Classic.Infrastructure.Extensions;
using Classic.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using Serilog.Core;
using Xunit;

namespace Classic.Infrastructure.Tests.Extensions;

public class MessageHandlerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageHandlers_WithCustomConfiguration_ShouldRegisterHandlersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(Logger.None); // Add mock logger
        services.AddSingleton<IMessageFormattingService>(Mock.Of<IMessageFormattingService>()); // Add mock formatting service

        // Act
        services.AddMessageHandlers(options =>
        {
            options.DefaultTarget = MessageTarget.Gui;
            options.RegisterHandler<ConsoleMessageHandler>(MessageTarget.Cli);
            options.RegisterHandler<GuiMessageHandler>(MessageTarget.Gui);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<MessageHandlerOptions>();
        options.DefaultTarget.Should().Be(MessageTarget.Gui);

        var factory = serviceProvider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();
        
        var cliHandler = factory(MessageTarget.Cli);
        cliHandler.Should().BeOfType<ConsoleMessageHandler>();

        var guiHandler = factory(MessageTarget.Gui);
        guiHandler.Should().BeOfType<GuiMessageHandler>();

        var defaultHandler = serviceProvider.GetRequiredService<IMessageHandler>();
        defaultHandler.Should().BeOfType<GuiMessageHandler>();
    }

    [Fact]
    public void AddMessageHandlers_WithBothTarget_ShouldFallbackToDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(Logger.None); // Add mock logger
        services.AddSingleton<IMessageFormattingService>(Mock.Of<IMessageFormattingService>()); // Add mock formatting service
        
        services.AddMessageHandlers(options =>
        {
            options.DefaultTarget = MessageTarget.Cli;
            options.RegisterHandler<ConsoleMessageHandler>(MessageTarget.Cli);
            options.RegisterHandler<GuiMessageHandler>(MessageTarget.Gui);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();
        var bothHandler = factory(MessageTarget.Both);

        // Assert
        bothHandler.Should().BeOfType<ConsoleMessageHandler>(); // Should fallback to CLI (default)
    }

    [Fact]
    public void AddDefaultMessageHandlers_ShouldUseStandardConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(Logger.None); // Add mock logger
        services.AddSingleton<IMessageFormattingService>(Mock.Of<IMessageFormattingService>()); // Add mock formatting service

        // Act
        services.AddDefaultMessageHandlers();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<MessageHandlerOptions>();
        options.DefaultTarget.Should().Be(MessageTarget.Cli);

        var factory = serviceProvider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();
        
        factory(MessageTarget.Cli).Should().BeOfType<ConsoleMessageHandler>();
        factory(MessageTarget.Gui).Should().BeOfType<GuiMessageHandler>();

        var defaultHandler = serviceProvider.GetRequiredService<IMessageHandler>();
        defaultHandler.Should().BeOfType<ConsoleMessageHandler>();
    }

    [Fact]
    public void AddMessageHandlers_WithUnknownTarget_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(Logger.None); // Add mock logger
        services.AddSingleton<IMessageFormattingService>(Mock.Of<IMessageFormattingService>()); // Add mock formatting service
        
        services.AddMessageHandlers(options =>
        {
            // Register NO handlers and set default to an unregistered target
            options.DefaultTarget = MessageTarget.Both; // Set to a target we won't register handlers for
            // Don't register any handlers
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();

        // Act & Assert
        var action = () => factory(MessageTarget.Gui);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("No message handler registered for target 'Gui' and no default handler available.");
    }

    [Fact]
    public void MessageHandlers_ShouldBeRegisteredAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(Logger.None); // Add mock logger
        services.AddSingleton<IMessageFormattingService>(Mock.Of<IMessageFormattingService>()); // Add mock formatting service
        
        services.AddDefaultMessageHandlers();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetRequiredService<Func<MessageTarget, IMessageHandler>>();
        var handler1 = factory(MessageTarget.Cli);
        var handler2 = factory(MessageTarget.Cli);

        // Assert
        handler1.Should().BeSameAs(handler2); // Same instance = singleton
    }
}
