using System.IO.Abstractions;
using Classic.Core.Interfaces;
using Classic.Infrastructure;
using Classic.ScanGame.Configuration;
using Serilog;

namespace Classic.ScanGame.Checkers;

/// <summary>
/// Checks and validates settings for Crash Generator (Buffout4) configuration.
/// </summary>
public class CrashgenChecker : ICrashgenChecker
{
    private readonly IFileSystem _fileSystem;
    private readonly IYamlSettingsCache _yamlSettings;
    private readonly IGlobalRegistry _globalRegistry;
    private readonly ILogger _logger;
    private readonly IMessageHandler _messageHandler;

    private string? _pluginsPath;
    private string _crashgenName = "Buffout4";
    private string? _configFile;
    private HashSet<string> _installedPlugins = new();
    private readonly List<string> _messageList = new();

    public CrashgenChecker(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGlobalRegistry globalRegistry,
        ILogger logger,
        IMessageHandler messageHandler)
    {
        _fileSystem = fileSystem;
        _yamlSettings = yamlSettings;
        _globalRegistry = globalRegistry;
        _logger = logger;
        _messageHandler = messageHandler;
    }

    /// <summary>
    /// Scans game files and configurations for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    public async Task<string> ScanAsync()
    {
        return await CheckCrashgenSettingsAsync();
    }

    /// <summary>
    /// Checks the settings for the crash generator configuration.
    /// </summary>
    /// <returns>The result of the crash generation settings check.</returns>
    public async Task<string> CheckCrashgenSettingsAsync()
    {
        try
        {
            await InitializeAsync();

            if (string.IsNullOrEmpty(_configFile))
            {
                _messageList.AddRange(new[]
                {
                    $"# [!] NOTICE : Unable to find the {_crashgenName} config file, settings check will be skipped. #\n",
                    $"  To ensure this check doesn't get skipped, {_crashgenName} has to be installed manually.\n",
                    "  [ If you are using Mod Organizer 2, you need to run CLASSIC through a shortcut in MO2. ]\n-----\n"
                });
                return string.Join("", _messageList);
            }

            _logger.Information("Checking {CrashgenName} settings in {ConfigFile}", _crashgenName, _configFile);
            await ProcessSettingsAsync();

            return string.Join("", _messageList);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking crashgen settings");
            return "❌ ERROR: Failed to check crash generator settings\n-----\n";
        }
    }

    private async Task InitializeAsync()
    {
        _pluginsPath = await GetPluginsPathAsync();
        _crashgenName = await GetCrashgenNameAsync();
        _configFile = await FindConfigFileAsync();
        _installedPlugins = await DetectInstalledPluginsAsync();
    }

    private async Task<string?> GetPluginsPathAsync()
    {
        var vrSuffix = _globalRegistry.IsVrMode ? "VR" : "";
        var key = $"Game{vrSuffix}_Info.Game_Folder_Plugins";
        return await _yamlSettings.GetSettingAsync<string>("Game_Local", key);
    }

    private async Task<string> GetCrashgenNameAsync()
    {
        var vrSuffix = _globalRegistry.IsVrMode ? "VR" : "";
        var key = $"Game{vrSuffix}_Info.CRASHGEN_LogName";
        var crashgenName = await _yamlSettings.GetSettingAsync<string>("Game", key);
        return crashgenName ?? "Buffout4";
    }

    private Task<string?> FindConfigFileAsync()
    {
        if (string.IsNullOrEmpty(_pluginsPath))
        {
            return Task.FromResult<string?>(null);
        }

        var crashgenTomlOg = _fileSystem.Path.Combine(_pluginsPath, "Buffout4", "config.toml");
        var crashgenTomlVr = _fileSystem.Path.Combine(_pluginsPath, "Buffout4.toml");

        // Check for missing config files
        if (!_fileSystem.File.Exists(crashgenTomlOg) || !_fileSystem.File.Exists(crashgenTomlVr))
        {
            _messageList.AddRange(new[]
            {
                $"# ❌ CAUTION : {_crashgenName.ToUpper()} TOML SETTINGS FILE NOT FOUND! #\n",
                $"Please recheck your {_crashgenName} installation and delete any obsolete files.\n-----\n"
            });
        }

        // Check for duplicate config files
        if (_fileSystem.File.Exists(crashgenTomlOg) && _fileSystem.File.Exists(crashgenTomlVr))
        {
            _messageList.AddRange(new[]
            {
                $"# ❌ CAUTION : BOTH VERSIONS OF {_crashgenName.ToUpper()} TOML SETTINGS FILES WERE FOUND! #\n",
                $"When editing {_crashgenName} toml settings, make sure you are editing the correct file.\n",
                $"Please recheck your {_crashgenName} installation and delete any obsolete files.\n-----\n"
            });
        }

        // Determine which config file to use
        if (_fileSystem.File.Exists(crashgenTomlOg))
        {
            return Task.FromResult<string?>(crashgenTomlOg);
        }

        if (_fileSystem.File.Exists(crashgenTomlVr))
        {
            return Task.FromResult<string?>(crashgenTomlVr);
        }

        return Task.FromResult<string?>(null);
    }

    private async Task<HashSet<string>> DetectInstalledPluginsAsync()
    {
        var xseFiles = new HashSet<string>();

        if (string.IsNullOrEmpty(_pluginsPath) || !_fileSystem.Directory.Exists(_pluginsPath))
        {
            return xseFiles;
        }

        try
        {
            var files = _fileSystem.Directory.EnumerateFiles(_pluginsPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = _fileSystem.Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    xseFiles.Add(fileName.ToLowerInvariant());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error accessing plugins directory: {PluginsPath}", _pluginsPath);
            await _messageHandler.SendMessageAsync($"Cannot access plugins directory: {ex.Message}");
        }

        return xseFiles;
    }

    private bool HasPlugin(params string[] pluginNames)
    {
        return pluginNames.Any(plugin => _installedPlugins.Contains(plugin.ToLowerInvariant()));
    }

    private async Task ProcessSettingsAsync()
    {
        if (string.IsNullOrEmpty(_configFile))
        {
            return;
        }

        var hasBakaScrapHeap = _installedPlugins.Contains("bakascrapheap.dll");
        var settings = GetSettingsToCheck();

        var tomlConfig = new TomlConfigurationManager(_fileSystem, _logger);

        foreach (var setting in settings)
        {
            var currentValue = await tomlConfig.GetSettingAsync(_configFile, setting.Section, setting.Key);

            // Special case for BakaScrapHeap with MemoryManager
            if (setting.SpecialCase == "bakascrapheap" && hasBakaScrapHeap && currentValue is true)
            {
                _messageList.AddRange(new[]
                {
                    $"# ❌ CAUTION : The Baka ScrapHeap Mod is installed, but is redundant with {_crashgenName} #\n",
                    $" FIX: Uninstall the Baka ScrapHeap Mod, this prevents conflicts with {_crashgenName}.\n-----\n"
                });
                continue;
            }

            // Check if condition is met and setting needs changing
            if (setting.Condition && !Equals(currentValue, setting.DesiredValue))
            {
                _messageList.AddRange(new[]
                {
                    $"# ❌ CAUTION : {setting.Description}, but {setting.Name} parameter is set to {currentValue} #\n",
                    $"    Auto Scanner will change this parameter to {setting.DesiredValue} {setting.Reason}.\n-----\n"
                });

                // Apply the change
                await tomlConfig.SetSettingAsync(_configFile, setting.Section, setting.Key, setting.DesiredValue);
                _logger.Information("Changed {SettingName} from {CurrentValue} to {DesiredValue}", 
                    setting.Name, currentValue, setting.DesiredValue);
            }
            else
            {
                // Setting is already correctly configured
                _messageList.Add($"✔️ {setting.Name} parameter is correctly configured in your {_crashgenName} settings!\n-----\n");
            }
        }
    }

    private List<CrashgenSetting> GetSettingsToCheck()
    {
        if (_globalRegistry.CurrentGame != "Fallout4")
        {
            return new List<CrashgenSetting>();
        }

        var hasXcell = HasPlugin("x-cell-fo4.dll", "x-cell-og.dll", "x-cell-ng2.dll");
        var hasAchievements = HasPlugin("achievements.dll", "achievementsmodsenablerloader.dll");
        var hasLooksMenu = _installedPlugins.Any(file => file.Contains("f4ee"));

        return new List<CrashgenSetting>
        {
            new("Patches", "Achievements", "Achievements", hasAchievements, false,
                "The Achievements Mod and/or Unlimited Survival Mode is installed",
                $"to prevent conflicts with {_crashgenName}"),
            new("Patches", "MemoryManager", "Memory Manager", hasXcell, false,
                "The X-Cell Mod is installed", "to prevent conflicts with X-Cell", "bakascrapheap"),
            new("Patches", "HavokMemorySystem", "Havok Memory System", hasXcell, false,
                "The X-Cell Mod is installed", "to prevent conflicts with X-Cell"),
            new("Patches", "BSTextureStreamerLocalHeap", "BS Texture Streamer Local Heap", hasXcell, false,
                "The X-Cell Mod is installed", "to prevent conflicts with X-Cell"),
            new("Patches", "ScaleformAllocator", "Scaleform Allocator", hasXcell, false,
                "The X-Cell Mod is installed", "to prevent conflicts with X-Cell"),
            new("Patches", "SmallBlockAllocator", "Small Block Allocator", hasXcell, false,
                "The X-Cell Mod is installed", "to prevent conflicts with X-Cell"),
            new("Patches", "ArchiveLimit", "Archive Limit", 
                !string.IsNullOrEmpty(_configFile) && _configFile.ToLowerInvariant().Contains("buffout4/config.toml"), false,
                "Archive Limit is enabled", "to prevent crashes"),
            new("Patches", "MaxStdIO", "MaxStdIO", false, 2048,
                "MaxStdIO is set to a low value", "to improve performance"),
            new("Compatibility", "F4EE", "F4EE (Looks Menu)", hasLooksMenu, true,
                "Looks Menu is installed, but F4EE parameter is set to FALSE",
                "to prevent bugs and crashes from Looks Menu")
        };
    }

    private record CrashgenSetting(
        string Section,
        string Key,
        string Name,
        bool Condition,
        object DesiredValue,
        string Description,
        string Reason,
        string? SpecialCase = null);
}