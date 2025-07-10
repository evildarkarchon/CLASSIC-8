namespace Classic.Core.Interfaces;

/// <summary>
/// Provides game-specific configuration information.
/// </summary>
public interface IGameConfiguration
{
    /// <summary>
    /// Gets whether the application is running in VR mode.
    /// </summary>
    bool IsVrMode { get; }

    /// <summary>
    /// Gets the current game name.
    /// </summary>
    string CurrentGame { get; }
}