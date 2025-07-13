using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic.Avalonia.ViewModels;
using Classic.Core.Models;
using System;

namespace Classic.Avalonia.Views;

public partial class PastebinDialog : UserControl
{
    public event EventHandler<PastebinResult>? LogFetched;
    public event EventHandler? DialogClosed;

    public PastebinDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Subscribe to ViewModel events
        if (DataContext is PastebinDialogViewModel viewModel)
        {
            viewModel.LogFetched += OnLogFetched;
            viewModel.DialogClosed += OnDialogClosed;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        // Unsubscribe from ViewModel events
        if (DataContext is PastebinDialogViewModel viewModel)
        {
            viewModel.LogFetched -= OnLogFetched;
            viewModel.DialogClosed -= OnDialogClosed;
        }
        
        base.OnUnloaded(e);
    }

    private void OnLogFetched(object? sender, PastebinResult result)
    {
        LogFetched?.Invoke(this, result);
    }

    private void OnDialogClosed(object? sender, EventArgs e)
    {
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }
}
