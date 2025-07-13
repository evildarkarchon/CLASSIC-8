using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Classic.Avalonia.ViewModels;
using Classic.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Classic.Avalonia;

public class App : Application
{
    public IServiceProvider? Services => Program.ServiceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Program.ServiceProvider != null)
        {
            var mainViewModel = Program.ServiceProvider.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
