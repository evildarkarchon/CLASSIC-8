using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Configuration;

/// <summary>
/// Provides game-specific configuration information loaded from YAML settings.
/// </summary>
public class GameConfiguration : IGameConfiguration
{
    private readonly ILogger _logger;
    private bool _isVrMode;
    private string _currentGame = string.Empty;

    public GameConfiguration(ISettingsService settingsService, ILogger logger)
    {
        _logger = logger;
        Initialize(settingsService);
    }

    /// <summary>
    /// Gets whether the application is running in VR mode.
    /// </summary>
    public bool IsVrMode => _isVrMode;

    /// <summary>
    /// Gets the current game name.
    /// </summary>
    public string CurrentGame => _currentGame;

    private void Initialize(ISettingsService settingsService)
    {
        try
        {
            // Use strongly-typed settings access for better performance
            var settings = settingsService.Settings;
            _isVrMode = settings.VRMode;
            _currentGame = settings.ManagedGame;

            _logger.Information("Game configuration initialized: VR Mode = {IsVrMode}, Current Game = {CurrentGame}",
                _isVrMode, _currentGame);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize game configuration, using defaults");
            _isVrMode = false;
            _currentGame = "Fallout4";
        }
    }
}
