using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic.Avalonia.ViewModels;
using System.Reactive;

namespace Classic.Avalonia.Views;

public partial class HelpDialog : Window
{
    public HelpDialog()
    {
        InitializeComponent();

        // Subscribe to close command if DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is HelpDialogViewModel viewModel)
            viewModel.CloseCommand.Subscribe(Observer.Create<Unit>(_ => Close()));
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Set focus to the topics list for better keyboard navigation
        var listBox = this.FindControl<ListBox>("TopicsList");
        listBox?.Focus();
    }
}
