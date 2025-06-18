namespace CLASSIC_8.Core;

/// <summary>
///     Default implementation of IGameManager.
/// </summary>
public class GameManager : IGameManager
{
    private string _currentGame = "Fallout4";

    public string CurrentGame
    {
        get => _currentGame;
        set => _currentGame = value ?? "Fallout4";
    }

    public bool IsVrMode { get; set; }

    public string GameIdentifier => IsVrMode ? $"{CurrentGame}VR" : CurrentGame;
}