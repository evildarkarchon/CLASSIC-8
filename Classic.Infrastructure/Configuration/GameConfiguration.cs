using Classic.Core.Enums;
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
        InitializeAsync(settingsService).Wait();
    }

    /// <summary>
    /// Gets whether the application is running in VR mode.
    /// </summary>
    public bool IsVrMode => _isVrMode;

    /// <summary>
    /// Gets the current game name.
    /// </summary>
    public string CurrentGame => _currentGame;

    private async Task InitializeAsync(ISettingsService settingsService)
    {
        try
        {
            // Load VR mode setting
            _isVrMode = await settingsService.GetSettingAsync<bool?>("VR Mode") ?? false;

            // Load current game setting
            _currentGame = await settingsService.GetSettingAsync<string>("Managed Game") ?? "Fallout4";

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
