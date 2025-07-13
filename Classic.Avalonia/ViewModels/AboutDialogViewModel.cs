using System.Reactive;
using System.Reflection;
using ReactiveUI;

namespace Classic.Avalonia.ViewModels;

public class AboutDialogViewModel : ViewModelBase
{
    public AboutDialogViewModel()
    {
        CloseCommand = ReactiveCommand.Create<Unit, Unit>(_ => Unit.Default);

        // Get version from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();
        Version = assemblyName.Version?.ToString() ?? "1.0.0.0";

        // Set application info
        ApplicationName = "CLASSIC-8";
        Description = "Crash Log Analyzer & Statistics Scanner for Bethesda Games";
        Copyright = "© 2024 CLASSIC-8 Contributors";

        // Build info
        var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);
        BuildInfo = $"Built on {buildDate:yyyy-MM-dd}";

        // Supported games
        SupportedGames = "Fallout 4, Fallout 4 VR, Skyrim SE, Skyrim VR";

        // Project links
        ProjectUrl = "https://github.com/yourusername/CLASSIC-8";
        OriginalProjectUrl = "https://github.com/originalpython/project";
    }

    public string ApplicationName { get; }
    public string Version { get; }
    public string Description { get; }
    public string Copyright { get; }
    public string BuildInfo { get; }
    public string SupportedGames { get; }
    public string ProjectUrl { get; }
    public string OriginalProjectUrl { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
}
