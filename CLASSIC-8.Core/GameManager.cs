namespace CLASSIC_8.Core;

/// <summary>
/// Default implementation of IGameManager.
/// </summary>
public class GameManager : IGameManager
{
    private string _currentGame = "Fallout4";
    private bool _isVrMode;
    
    public string CurrentGame
    {
        get => _currentGame;
        set => _currentGame = value ?? "Fallout4";
    }
    
    public bool IsVrMode
    {
        get => _isVrMode;
        set => _isVrMode = value;
    }
    
    public string GameIdentifier => IsVrMode ? $"{CurrentGame}VR" : CurrentGame;
}