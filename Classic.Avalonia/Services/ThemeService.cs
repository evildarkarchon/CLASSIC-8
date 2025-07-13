using Avalonia.Styling;
using Classic.Core.Interfaces;
using System;
using System.Threading.Tasks;
using Serilog;

namespace Classic.Avalonia.Services;

/// <summary>
/// Interface for theme management services.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme variant.
    /// </summary>
    ThemeVariant CurrentTheme { get; }
    
    /// <summary>
    /// Sets the theme for the application.
    /// </summary>
    /// <param name="themeName">Theme name (Dark, Light, Auto)</param>
    Task SetThemeAsync(string themeName);
    
    /// <summary>
    /// Initializes the theme service and applies the saved theme.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    Task ToggleThemeAsync();
    
    /// <summary>
    /// Event fired when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

/// <summary>
/// Event arguments for theme changed events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public string ThemeName { get; }
    public ThemeVariant ThemeVariant { get; }
    
    public ThemeChangedEventArgs(string themeName, ThemeVariant themeVariant)
    {
        ThemeName = themeName;
        ThemeVariant = themeVariant;
    }
}

/// <summary>
/// Service for managing application themes.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private ThemeVariant _currentTheme = ThemeVariant.Dark;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService(ISettingsService settingsService, ILogger logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public ThemeVariant CurrentTheme => _currentTheme;

    public async Task InitializeAsync()
    {
        try
        {
            var themeName = _settingsService.Settings.Theme.CurrentTheme;
            await ApplyTheme(themeName);
            _logger.Information("Theme service initialized with theme: {ThemeName}", themeName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize theme service");
            // Fall back to dark theme
            await ApplyTheme("Dark");
        }
    }

    public async Task SetThemeAsync(string themeName)
    {
        try
        {
            await ApplyTheme(themeName);
            
            // Save to settings
            _settingsService.Settings.Theme.CurrentTheme = themeName;
            await _settingsService.SaveAsync();
            
            _logger.Information("Theme changed to: {ThemeName}", themeName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set theme: {ThemeName}", themeName);
            throw;
        }
    }

    public async Task ToggleThemeAsync()
    {
        var currentThemeName = _settingsService.Settings.Theme.CurrentTheme;
        var newTheme = currentThemeName switch
        {
            "Light" => "Dark",
            "Dark" => "Light",
            "Auto" => "Dark", // Default toggle behavior for Auto
            _ => "Dark"
        };
        
        await SetThemeAsync(newTheme);
    }

    private async Task ApplyTheme(string themeName)
    {
        var app = global::Avalonia.Application.Current;
        if (app == null)
        {
            _logger.Warning("Application instance not available for theme change");
            return;
        }

        var themeVariant = themeName switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "Auto" => DetectSystemTheme(),
            _ => ThemeVariant.Dark
        };

        _currentTheme = themeVariant;
        app.RequestedThemeVariant = themeVariant;
        
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(themeName, themeVariant));
        
        await Task.CompletedTask; // For future async theme operations
    }

    private static ThemeVariant DetectSystemTheme()
    {
        // For now, default to Dark. In the future, we could implement system theme detection
        // This would require platform-specific code for Windows, macOS, and Linux
        return ThemeVariant.Dark;
    }
}
