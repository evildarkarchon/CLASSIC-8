using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Classic.Avalonia.Controls;
using Classic.Avalonia.ViewModels;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Classic.Avalonia.Views;

public partial class MainWindow : Window
{
    private ToastContainer? _toastContainer;
    private INotificationService? _notificationService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize toast container
        _toastContainer = new ToastContainer();
        ToastContainer.Content = _toastContainer;
        
        // Improve window sizing for different screen configurations
        ConfigureWindowSizing();
    }
    
    private void ConfigureWindowSizing()
    {
        // Get screen information
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            
            // Calculate appropriate window size (80% of working area, but not larger than design size)
            var maxWidth = Math.Min(1400, (int)(workingArea.Width * 0.8 / scaling));
            var maxHeight = Math.Min(900, (int)(workingArea.Height * 0.8 / scaling));
            
            // Set responsive window size  
            Width = Math.Max(MinWidth, Math.Min(maxWidth, 700));
            Height = Math.Max(MinHeight, Math.Min(maxHeight, 725));
            
            // Ensure window fits on screen
            if (Width > workingArea.Width / scaling)
                Width = workingArea.Width / scaling * 0.9;
            if (Height > workingArea.Height / scaling)
                Height = workingArea.Height / scaling * 0.9;
        }
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        // Get notification service from DI container when DataContext is set
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Try to get the notification service from the application's service provider
            var app = global::Avalonia.Application.Current as App;
            if (app?.Services != null)
            {
                _notificationService = app.Services.GetService<INotificationService>();
                if (_notificationService != null)
                {
                    // Subscribe to notifications
                    ((Infrastructure.Services.NotificationService)_notificationService).NotificationAdded += OnNotificationAdded;
                }
            }
        }
    }
    
    private async void OnNotificationAdded(object? sender, NotificationMessage notification)
    {
        if (_toastContainer != null)
        {
            await _toastContainer.ShowToastAsync(notification);
        }
    }
}
