using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Classic.Core.Models;
using System;

namespace Classic.Avalonia.Controls;

public partial class ToastNotification : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ToastNotification, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ToastNotification, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<NotificationType> NotificationTypeProperty =
        AvaloniaProperty.Register<ToastNotification, NotificationType>(nameof(NotificationType), NotificationType.Information);

    public event EventHandler<EventArgs>? CloseRequested;

    public ToastNotification()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotificationType NotificationType
    {
        get => GetValue(NotificationTypeProperty);
        set => SetValue(NotificationTypeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            TitleTextBlock.Text = Title;
        }
        else if (change.Property == MessageProperty)
        {
            MessageTextBlock.Text = Message;
        }
        else if (change.Property == NotificationTypeProperty)
        {
            UpdateNotificationTypeStyle();
        }
    }

    private void UpdateNotificationTypeStyle()
    {
        // Remove existing type classes
        ToastBorder.Classes.Remove("success");
        ToastBorder.Classes.Remove("warning");
        ToastBorder.Classes.Remove("error");
        ToastBorder.Classes.Remove("information");

        // Add appropriate class based on notification type
        var typeClass = NotificationType switch
        {
            NotificationType.Success => "success",
            NotificationType.Warning => "warning",
            NotificationType.Error => "error",
            NotificationType.Information => "information",
            _ => "information"
        };

        ToastBorder.Classes.Add(typeClass);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetNotification(NotificationMessage notification)
    {
        Title = notification.Title;
        Message = notification.Message;
        NotificationType = notification.Type;
    }
}