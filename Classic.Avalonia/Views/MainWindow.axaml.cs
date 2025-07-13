using Avalonia.Controls;
using Classic.Avalonia.Controls;
using Classic.Avalonia.ViewModels;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using Avalonia.Input;
using Classic.Avalonia.Services;
using Serilog;

namespace Classic.Avalonia.Views;

public partial class MainWindow : Window
{
    private ToastContainer? _toastContainer;
    private INotificationService? _notificationService;
    private IWindowStateService? _windowStateService;
    private IDragDropService? _dragDropService;
    private ILogger? _logger;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize toast container
        _toastContainer = new ToastContainer();
        ToastContainer.Content = _toastContainer;

        // Improve window sizing for different screen configurations
        ConfigureWindowSizing();

        // Set up event handlers
        Closing += OnWindowClosing;
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object? sender, EventArgs e)
    {
        // Initialize services when window is loaded
        InitializeServices();

        // Set up drag and drop events
        SetupDragAndDrop();

        // Restore window state
        if (_windowStateService != null)
        {
            await _windowStateService.RestoreWindowStateAsync(this);
        }
    }

    private void SetupDragAndDrop()
    {
        // Set up drag and drop events on the window
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Save window state before closing
        if (_windowStateService != null)
        {
            await _windowStateService.SaveWindowStateAsync(this);
        }
    }

    private void InitializeServices()
    {
        // Get services from DI container when DataContext is set
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Try to get services from the application's service provider
            var app = global::Avalonia.Application.Current as App;
            if (app?.Services != null)
            {
                _notificationService = app.Services.GetService<INotificationService>();
                _windowStateService = app.Services.GetService<IWindowStateService>();
                _dragDropService = app.Services.GetService<IDragDropService>();
                _logger = app.Services.GetService<ILogger>();

                // Subscribe to notification events
                if (_notificationService != null)
                {
                    _notificationService.NotificationAdded += OnNotificationAdded;
                }
            }
        }
    }

    private void ConfigureWindowSizing()
    {
        // Get screen information
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;

            // Get the initial width and height from AXAML metadata
            var initialWidth = Width;
            var initialHeight = Height;

            // Calculate appropriate window size (80% of working area, but not larger than design size)
            var maxWidth = Math.Min(1400, (int)(workingArea.Width * 0.8 / scaling));
            var maxHeight = Math.Min(900, (int)(workingArea.Height * 0.8 / scaling));

            // Set responsive window size using AXAML-defined values
            var newWidth = Math.Max(MinWidth, Math.Min(maxWidth, initialWidth));
            var newHeight = Math.Max(MinHeight, Math.Min(maxHeight, initialHeight));

            // Ensure window fits on screen
            if (newWidth > workingArea.Width / scaling)
                newWidth = workingArea.Width / scaling * 0.9;
            if (newHeight > workingArea.Height / scaling)
                newHeight = workingArea.Height / scaling * 0.9;

            // Apply the new dimensions
            Width = newWidth;
            Height = newHeight;
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
                    ((Infrastructure.Services.NotificationService)_notificationService).NotificationAdded +=
                        OnNotificationAdded;
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

    #region Drag and Drop Event Handlers

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (_dragDropService?.ValidateDropData(e) == true)
        {
            e.DragEffects = DragDropEffects.Copy;
            AddDropZoneStyle();
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_dragDropService?.ValidateDropData(e) == true)
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        RemoveDropZoneStyle();
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        RemoveDropZoneStyle();

        if (_dragDropService != null)
        {
            try
            {
                await _dragDropService.ProcessDroppedFilesAsync(e);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error processing dropped files");
            }
        }
    }

    private void AddDropZoneStyle()
    {
        // Add visual feedback for drag-over state
        Classes.Add("drag-over");
    }

    private void RemoveDropZoneStyle()
    {
        // Remove visual feedback
        Classes.Remove("drag-over");
    }

    #endregion
}
