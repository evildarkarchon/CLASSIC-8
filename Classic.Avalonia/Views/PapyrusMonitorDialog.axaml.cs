using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic.Avalonia.ViewModels;

namespace Classic.Avalonia.Views;

public partial class PapyrusMonitorDialog : UserControl
{
    public PapyrusMonitorDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Subscribe to dialog close events if needed
        if (DataContext is PapyrusMonitorDialogViewModel viewModel)
        {
            // Additional initialization if needed
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        // Clean up if needed
        if (DataContext is PapyrusMonitorDialogViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnUnloaded(e);
    }
}
