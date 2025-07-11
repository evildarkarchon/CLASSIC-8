namespace Classic.Core.Enums;

/// <summary>
/// Defines the different YAML configuration stores used by the application.
/// </summary>
public enum YamlStore
{
    /// <summary>
    /// Main configuration database (CLASSIC Data/databases/CLASSIC Main.yaml).
    /// Contains application metadata and default settings.
    /// </summary>
    Main,

    /// <summary>
    /// User settings file (CLASSIC Settings.yaml).
    /// Contains user preferences and application configuration.
    /// </summary>
    Settings,

    /// <summary>
    /// Ignore lists configuration (CLASSIC Ignore.yaml).
    /// Contains lists of plugins to ignore per game.
    /// </summary>
    Ignore,

    /// <summary>
    /// Game-specific configuration (CLASSIC Data/databases/CLASSIC {Game}.yaml).
    /// Contains game-specific data and patterns.
    /// </summary>
    Game,

    /// <summary>
    /// Local game configuration (CLASSIC Data/CLASSIC {Game} Local.yaml).
    /// Contains user-specific game settings.
    /// </summary>
    GameLocal,

    /// <summary>
    /// Test configuration for unit testing.
    /// </summary>
    Test
}