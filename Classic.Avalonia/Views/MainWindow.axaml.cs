using Avalonia.Controls;
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
