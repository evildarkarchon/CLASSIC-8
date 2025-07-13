using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Classic.Avalonia.Extensions;
using Classic.Avalonia.Services;
using Classic.Avalonia.ViewModels;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Extensions;
using Classic.ScanLog.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Classic.Infrastructure.Platform;

namespace Classic.Avalonia;

internal sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        // Run platform compatibility check
        PlatformCompatibilityChecker.CheckCompatibility();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Add infrastructure services (includes logging, YAML settings, etc.)
        services.AddClassicInfrastructure();
        
        // Add scan log services (orchestration, parsers, analyzers, etc.)
        services.AddScanLogServices();
        
        // Add Avalonia UI services (theme, window state, drag-drop)
        services.AddAvaloniaUIServices();
        
        // Add ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        // Keep MockScanOrchestrator available for testing if needed
        services.AddTransient<MockScanOrchestrator>();
    }
}