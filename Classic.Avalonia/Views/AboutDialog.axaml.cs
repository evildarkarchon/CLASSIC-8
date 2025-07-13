using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic.Avalonia.ViewModels;
using System.Reactive;

namespace Classic.Avalonia.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        // Subscribe to close command if DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is AboutDialogViewModel viewModel)
            viewModel.CloseCommand.Subscribe(Observer.Create<Unit>(_ => Close()));
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Set focus to close button for better keyboard navigation
        var closeButton = this.FindControl<Button>("CloseButton");
        closeButton?.Focus();
    }
}
