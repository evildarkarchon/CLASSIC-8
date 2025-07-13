namespace Classic.Core.Models.Settings;

/// <summary>
/// Represents the main application settings stored in CLASSIC Settings.yaml.
/// </summary>
public class ClassicSettings
{
    /// <summary>
    /// The currently managed game.
    /// </summary>
    public string ManagedGame { get; set; } = "Fallout 4";

    /// <summary>
    /// Whether to check for application updates.
    /// </summary>
    public bool UpdateCheck { get; set; } = true;

    /// <summary>
    /// Whether to prioritize VR version of the game.
    /// </summary>
    public bool VRMode { get; set; } = false;

    /// <summary>
    /// Whether FCX (File Check Xtended) mode is enabled.
    /// </summary>
    public bool FCXMode { get; set; } = true;

    /// <summary>
    /// Whether to simplify crash logs by removing redundant information.
    /// </summary>
    public bool SimplifyLogs { get; set; } = false;

    /// <summary>
    /// Whether to play a sound on scan completion.
    /// </summary>
    public bool SoundOnCompletion { get; set; } = true;

    /// <summary>
    /// Whether to scan INI files along with crash logs.
    /// </summary>
    public bool ScanGameFiles { get; set; } = false;

    /// <summary>
    /// Whether to exclude scan results containing only warnings.
    /// </summary>
    public bool ExcludeWarnings { get; set; } = false;

    /// <summary>
    /// Whether to use alpha/beta update sources.
    /// </summary>
    public bool UsePreReleases { get; set; } = false;

    /// <summary>
    /// The source for updates (GitHub, Nexus, etc.).
    /// </summary>
    public string UpdateSource { get; set; } = "GitHub";

    /// <summary>
    /// Path to the staging mods folder.
    /// </summary>
    public string? StagingModsPath { get; set; }

    /// <summary>
    /// Path to custom scan folder.
    /// </summary>
    public string? CustomScanPath { get; set; }

    /// <summary>
    /// Path to the game's INI folder.
    /// </summary>
    public string? GameIniPath { get; set; }

    /// <summary>
    /// Window state persistence settings.
    /// </summary>
    public WindowStateSettings WindowState { get; set; } = new();

    /// <summary>
    /// Theme settings for the application.
    /// </summary>
    public ThemeSettings Theme { get; set; } = new();
}

/// <summary>
/// Settings for window state persistence.
/// </summary>
public class WindowStateSettings
{
    /// <summary>
    /// Window X position.
    /// </summary>
    public double? X { get; set; }

    /// <summary>
    /// Window Y position.
    /// </summary>
    public double? Y { get; set; }

    /// <summary>
    /// Window width.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Window height.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// Window state (Normal, Minimized, Maximized).
    /// </summary>
    public string WindowState { get; set; } = "Normal";

    /// <summary>
    /// Selected tab index.
    /// </summary>
    public int SelectedTabIndex { get; set; } = 0;
}

/// <summary>
/// Theme settings for the application.
/// </summary>
public class ThemeSettings
{
    /// <summary>
    /// Current theme name (Dark, Light, Auto).
    /// </summary>
    public string CurrentTheme { get; set; } = "Dark";

    /// <summary>
    /// Whether to follow system theme automatically.
    /// </summary>
    public bool FollowSystemTheme { get; set; } = false;
}