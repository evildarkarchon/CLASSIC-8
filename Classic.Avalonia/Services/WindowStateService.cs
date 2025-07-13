using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Classic.Core.Interfaces;
using System;
using System.Threading.Tasks;
using Serilog;

namespace Classic.Avalonia.Services;

/// <summary>
/// Interface for window state management services.
/// </summary>
public interface IWindowStateService
{
    /// <summary>
    /// Saves the current window state to settings.
    /// </summary>
    /// <param name="window">The window to save state for</param>
    Task SaveWindowStateAsync(Window window);
    
    /// <summary>
    /// Restores the window state from settings.
    /// </summary>
    /// <param name="window">The window to restore state for</param>
    Task RestoreWindowStateAsync(Window window);
    
    /// <summary>
    /// Saves the selected tab index.
    /// </summary>
    /// <param name="tabIndex">The tab index to save</param>
    Task SaveSelectedTabAsync(int tabIndex);
    
    /// <summary>
    /// Gets the saved selected tab index.
    /// </summary>
    /// <returns>The saved tab index, or 0 if none saved</returns>
    int GetSelectedTab();
}

/// <summary>
/// Service for managing window state persistence.
/// </summary>
public class WindowStateService : IWindowStateService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;

    public WindowStateService(ISettingsService settingsService, ILogger logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task SaveWindowStateAsync(Window window)
    {
        try
        {
            var settings = _settingsService.Settings.WindowState;
            
            // Save position and size if the window is in normal state
            if (window.WindowState == WindowState.Normal)
            {
                settings.X = window.Position.X;
                settings.Y = window.Position.Y;
                settings.Width = window.Width;
                settings.Height = window.Height;
            }
            
            // Save window state
            settings.WindowState = window.WindowState.ToString();
            
            await _settingsService.SaveAsync();
            
            _logger.Debug("Window state saved: Position=({X},{Y}), Size=({Width}x{Height}), State={State}",
                settings.X, settings.Y, settings.Width, settings.Height, settings.WindowState);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save window state");
        }
    }

    public async Task RestoreWindowStateAsync(Window window)
    {
        try
        {
            var settings = _settingsService.Settings.WindowState;
            
            // Restore position and size if available
            if (settings.X.HasValue && settings.Y.HasValue)
            {
                var position = new PixelPoint((int)settings.X.Value, (int)settings.Y.Value);
                
                // Validate that the position is within screen bounds
                if (IsPositionValid(position))
                {
                    window.Position = position;
                }
            }
            
            if (settings.Width.HasValue && settings.Height.HasValue)
            {
                // Ensure size is within reasonable bounds
                var width = Math.Max(window.MinWidth, Math.Min(2560, settings.Width.Value));
                var height = Math.Max(window.MinHeight, Math.Min(1440, settings.Height.Value));
                
                window.Width = width;
                window.Height = height;
            }
            
            // Restore window state
            if (Enum.TryParse<WindowState>(settings.WindowState, out var windowState))
            {
                window.WindowState = windowState;
            }
            
            _logger.Debug("Window state restored: Position=({X},{Y}), Size=({Width}x{Height}), State={State}",
                settings.X, settings.Y, settings.Width, settings.Height, settings.WindowState);
                
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restore window state");
        }
    }

    public async Task SaveSelectedTabAsync(int tabIndex)
    {
        try
        {
            _settingsService.Settings.WindowState.SelectedTabIndex = tabIndex;
            await _settingsService.SaveAsync();
            
            _logger.Debug("Selected tab saved: {TabIndex}", tabIndex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save selected tab");
        }
    }

    public int GetSelectedTab()
    {
        try
        {
            return _settingsService.Settings.WindowState.SelectedTabIndex;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get selected tab");
            return 0;
        }
    }

    private static bool IsPositionValid(PixelPoint position)
    {
        // For now, just return true to allow any position
        // In a future version, we could implement proper screen bounds checking
        return true;
    }
}
