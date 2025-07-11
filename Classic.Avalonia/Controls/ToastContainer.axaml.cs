using Avalonia.Controls;
using Avalonia.Threading;
using Classic.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Classic.Avalonia.Controls;

public partial class ToastContainer : UserControl
{
    private readonly List<ToastNotification> _activeToasts = new();
    private readonly int _maxConcurrentToasts = 3;

    public ToastContainer()
    {
        InitializeComponent();
    }

    public async Task ShowToastAsync(NotificationMessage notification)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Remove oldest toast if we're at max capacity
            if (_activeToasts.Count >= _maxConcurrentToasts)
            {
                var oldestToast = _activeToasts.First();
                RemoveToast(oldestToast);
            }

            // Create new toast
            var toast = new ToastNotification();
            toast.SetNotification(notification);
            toast.CloseRequested += (sender, e) =>
            {
                if (sender is ToastNotification toastToRemove)
                {
                    RemoveToast(toastToRemove);
                }
            };

            // Add to UI and tracking
            ToastStackPanel.Children.Add(toast);
            _activeToasts.Add(toast);

            // Auto-remove after duration
            var timer = new System.Timers.Timer(notification.Duration.TotalMilliseconds);
            timer.Elapsed += (sender, e) =>
            {
                timer.Dispose();
                Dispatcher.UIThread.InvokeAsync(() => RemoveToast(toast));
            };
            timer.Start();
        });
    }

    private void RemoveToast(ToastNotification toast)
    {
        if (_activeToasts.Contains(toast))
        {
            _activeToasts.Remove(toast);
            ToastStackPanel.Children.Remove(toast);
        }
    }

    public void ClearAllToasts()
    {
        var toastsToRemove = _activeToasts.ToList();
        foreach (var toast in toastsToRemove)
        {
            RemoveToast(toast);
        }
    }
}