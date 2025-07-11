using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Classic.Avalonia.ViewModels;
using Classic.Avalonia.Views;
using Classic.Avalonia.Services;
using Serilog;

namespace Classic.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    new MockScanOrchestrator(),
                    Log.Logger ?? new LoggerConfiguration().CreateLogger()
                )
            };

        base.OnFrameworkInitializationCompleted();
    }
}
