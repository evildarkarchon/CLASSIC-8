namespace CLASSIC_8.Core;

/// <summary>
/// Manages the currently selected game and VR mode.
/// </summary>
public interface IGameManager
{
    /// <summary>
    /// Gets or sets the currently selected game.
    /// </summary>
    string CurrentGame { get; set; }
    
    /// <summary>
    /// Gets or sets whether VR mode is enabled.
    /// </summary>
    bool IsVrMode { get; set; }
    
    /// <summary>
    /// Gets the game identifier used for file paths and configurations.
    /// Includes VR suffix if VR mode is enabled.
    /// </summary>
    string GameIdentifier { get; }
}