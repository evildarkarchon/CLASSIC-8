using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System;
using Classic.Infrastructure.Platform;

namespace Classic.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly IGameFileManager _gameFileManager;
    private readonly INotificationService _notificationService;
    private readonly IProgressService _progressService;
    private readonly IUpdateService _updateService;
    
    private string _selectedModsFolder = string.Empty;
    private string _selectedScanFolder = string.Empty;
    private bool _fcxMode;
    private bool _simplifyLogs;
    private bool _updateCheck = true;
    private bool _vrMode;
    private bool _showFormIdValues;
    private bool _moveInvalidLogs;
    private bool _audioNotifications = true;
    private string _updateSource = "Both";
    private bool _isScanning;
    private int _selectedTabIndex;
    
    // Progress tracking
    private int _progressPercentage;
    private string _progressMessage = string.Empty;
    private string _progressDetails = string.Empty;
    private bool _isProgressIndeterminate;
    private TimeSpan? _estimatedTimeRemaining;
    
    // Scan results
    private ScanResult? _lastScanResult;
    private string _scanStatistics = string.Empty;
    
    public MainWindowViewModel(IScanOrchestrator scanOrchestrator, ILogger logger, ISettingsService settingsService, IGameFileManager gameFileManager, INotificationService notificationService, IProgressService progressService, IUpdateService updateService)
    {
        _scanOrchestrator = scanOrchestrator;
        _logger = logger;
        _settingsService = settingsService;
        _gameFileManager = gameFileManager;
        _notificationService = notificationService;
        _progressService = progressService;
        _updateService = updateService;
        
        // Initialize commands
        ScanCrashLogsCommand = ReactiveCommand.CreateFromTask(ExecuteScanCrashLogs, this.WhenAnyValue(x => x.IsScanning, scanning => !scanning));
        ScanGameFilesCommand = ReactiveCommand.CreateFromTask(ExecuteScanGameFiles, this.WhenAnyValue(x => x.IsScanning, scanning => !scanning));
        SelectModsFolderCommand = ReactiveCommand.CreateFromTask(SelectModsFolder);
        SelectScanFolderCommand = ReactiveCommand.CreateFromTask(SelectScanFolder);
        SelectIniFolderCommand = ReactiveCommand.CreateFromTask(SelectIniFolder);
        OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
        OpenCrashLogsFolderCommand = ReactiveCommand.Create(OpenCrashLogsFolder);
        CheckUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
        ShowAboutCommand = ReactiveCommand.Create(ShowAbout);
        ShowHelpCommand = ReactiveCommand.Create(ShowHelp);
        ExitCommand = ReactiveCommand.Create(Exit);
        
        // Backup commands for each category
        BackupXseCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("XSE", "BACKUP"));
        RestoreXseCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("XSE", "RESTORE"));
        RemoveXseCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("XSE", "REMOVE"));
        
        BackupReshadeCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("RESHADE", "BACKUP"));
        RestoreReshadeCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("RESHADE", "RESTORE"));
        RemoveReshadeCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("RESHADE", "REMOVE"));
        
        BackupVulkanCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("VULKAN", "BACKUP"));
        RestoreVulkanCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("VULKAN", "RESTORE"));
        RemoveVulkanCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("VULKAN", "REMOVE"));
        
        BackupEnbCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("ENB", "BACKUP"));
        RestoreEnbCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("ENB", "RESTORE"));
        RemoveEnbCommand = ReactiveCommand.CreateFromTask(() => ManageGameFiles("ENB", "REMOVE"));
        
        // Initialize resource links
        InitializeResourceLinks();
        
        // Subscribe to progress updates
        _progressService.ProgressUpdated += OnProgressUpdated;
        
        // Load settings
        LoadSettings();
    }
    
    #region Properties
    
    public string SelectedModsFolder
    {
        get => _selectedModsFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedModsFolder, value);
            _ = SaveSettings();
        }
    }
    
    public string SelectedScanFolder
    {
        get => _selectedScanFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScanFolder, value);
            _ = SaveSettings();
        }
    }
    
    public bool FcxMode
    {
        get => _fcxMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _fcxMode, value);
            _ = SaveSettings();
        }
    }
    
    public bool SimplifyLogs
    {
        get => _simplifyLogs;
        set
        {
            this.RaiseAndSetIfChanged(ref _simplifyLogs, value);
            _ = SaveSettings();
        }
    }
    
    public bool UpdateCheck
    {
        get => _updateCheck;
        set
        {
            this.RaiseAndSetIfChanged(ref _updateCheck, value);
            _ = SaveSettings();
        }
    }
    
    public bool VrMode
    {
        get => _vrMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _vrMode, value);
            _ = SaveSettings();
        }
    }
    
    public bool ShowFormIdValues
    {
        get => _showFormIdValues;
        set => this.RaiseAndSetIfChanged(ref _showFormIdValues, value);
    }
    
    public bool MoveInvalidLogs
    {
        get => _moveInvalidLogs;
        set => this.RaiseAndSetIfChanged(ref _moveInvalidLogs, value);
    }
    
    public bool AudioNotifications
    {
        get => _audioNotifications;
        set
        {
            this.RaiseAndSetIfChanged(ref _audioNotifications, value);
            _ = SaveSettings();
        }
    }
    
    public string UpdateSource
    {
        get => _updateSource;
        set
        {
            this.RaiseAndSetIfChanged(ref _updateSource, value);
            _ = SaveSettings();
        }
    }
    
    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }
    
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }
    
    public ObservableCollection<string> UpdateSourceOptions { get; } = new()
    {
        "Nexus", "GitHub", "Both"
    };
    
    public ObservableCollection<ResourceLinkViewModel> ResourceLinks { get; } = new();
    
    // Progress properties
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
    }
    
    public string ProgressMessage
    {
        get => _progressMessage;
        set => this.RaiseAndSetIfChanged(ref _progressMessage, value);
    }
    
    public string ProgressDetails
    {
        get => _progressDetails;
        set => this.RaiseAndSetIfChanged(ref _progressDetails, value);
    }
    
    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _isProgressIndeterminate, value);
    }
    
    public TimeSpan? EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set => this.RaiseAndSetIfChanged(ref _estimatedTimeRemaining, value);
    }
    
    public ScanResult? LastScanResult
    {
        get => _lastScanResult;
        set => this.RaiseAndSetIfChanged(ref _lastScanResult, value);
    }
    
    public string ScanStatistics
    {
        get => _scanStatistics;
        set => this.RaiseAndSetIfChanged(ref _scanStatistics, value);
    }
    
    #endregion
    
    #region Commands
    
    public ReactiveCommand<Unit, Unit> ScanCrashLogsCommand { get; }
    public ReactiveCommand<Unit, Unit> ScanGameFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectModsFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectScanFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectIniFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCrashLogsFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowHelpCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    
    // Backup commands
    public ReactiveCommand<Unit, Unit> BackupXseCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreXseCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveXseCommand { get; }
    
    public ReactiveCommand<Unit, Unit> BackupReshadeCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreReshadeCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveReshadeCommand { get; }
    
    public ReactiveCommand<Unit, Unit> BackupVulkanCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreVulkanCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveVulkanCommand { get; }
    
    public ReactiveCommand<Unit, Unit> BackupEnbCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreEnbCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveEnbCommand { get; }
    
    #endregion
    
    #region Command Implementations
    
    private async Task ExecuteScanCrashLogs()
    {
        IsScanning = true;
        try
        {
            _logger.Information("Starting crash logs scan");
            _progressService.StartProgress("Scanning Crash Logs");
            
            var scanRequest = new ScanRequest
            {
                ModsPath = !string.IsNullOrWhiteSpace(SelectedModsFolder) ? SelectedModsFolder : null,
                EnableFcxMode = FcxMode,
                SimplifyLogs = SimplifyLogs,
                ShowFormIdValues = ShowFormIdValues,
                MoveUnsolvedLogs = MoveInvalidLogs,
                OutputDirectory = CrossPlatformHelper.SafePathCombine(CrossPlatformHelper.GetApplicationDataDirectory(), "Classic", "CrashLogs")
            };
            
            // Add custom scan folder to log files if specified
            if (!string.IsNullOrWhiteSpace(SelectedScanFolder))
            {
                // TODO: Enumerate crash log files from custom folder
            }
            
            var result = await _scanOrchestrator.ExecuteScanAsync(scanRequest);
            
            _logger.Information("Crash logs scan completed successfully");
            _progressService.CompleteProgress("Scan completed successfully");
            
            // Store results and update statistics
            LastScanResult = result;
            UpdateScanStatistics(result);
            
            // Show completion notification
            await _notificationService.ShowScanCompletedAsync(result);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Error during crash logs scan");
            _progressService.FailProgress($"Scan failed: {ex.Message}");
            await _notificationService.ShowScanErrorAsync("Crash Log Scan", ex);
        }
        finally
        {
            IsScanning = false;
        }
    }
    
    private async Task ExecuteScanGameFiles()
    {
        IsScanning = true;
        try
        {
            _logger.Information("Starting game files scan");
            _progressService.StartProgress("Scanning Game Files");
            
            var scanRequest = new ScanRequest
            {
                ModsPath = !string.IsNullOrWhiteSpace(SelectedModsFolder) ? SelectedModsFolder : null,
                EnableFcxMode = FcxMode,
                OutputDirectory = CrossPlatformHelper.SafePathCombine(CrossPlatformHelper.GetApplicationDataDirectory(), "Classic", "GameFiles")
            };
            
            var result = await _scanOrchestrator.ExecuteScanAsync(scanRequest);
            
            _logger.Information("Game files scan completed successfully");
            _progressService.CompleteProgress("Game files scan completed successfully");
            
            // Store results and update statistics
            LastScanResult = result;
            UpdateScanStatistics(result);
            
            // Show completion notification
            await _notificationService.ShowScanCompletedAsync(result);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Error during game files scan");
            _progressService.FailProgress($"Game files scan failed: {ex.Message}");
            await _notificationService.ShowScanErrorAsync("Game Files Scan", ex);
        }
        finally
        {
            IsScanning = false;
        }
    }
    
    private async Task SelectModsFolder()
    {
        _logger.Information("Opening mods folder selection dialog");
        
        try
        {
            var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (topLevel?.StorageProvider == null)
            {
                _logger.Warning("Storage provider not available for folder selection");
                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Staging Mods Folder",
                AllowMultiple = false
            };

            // Set suggested start location if current folder exists
            if (!string.IsNullOrWhiteSpace(SelectedModsFolder) && Directory.Exists(SelectedModsFolder))
            {
                var currentFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(SelectedModsFolder);
                if (currentFolder != null)
                {
                    options.SuggestedStartLocation = currentFolder;
                }
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            
            if (folders.Count > 0)
            {
                var selectedPath = folders[0].Path.LocalPath;
                
                // Validate the selected path
                if (ValidateFolder(selectedPath, "staging mods"))
                {
                    SelectedModsFolder = selectedPath;
                    _logger.Information("Selected mods folder: {Path}", selectedPath);
                    
                    // TODO: Save to settings
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error selecting mods folder");
        }
    }
    
    private async Task SelectScanFolder()
    {
        _logger.Information("Opening custom scan folder selection dialog");
        
        try
        {
            var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (topLevel?.StorageProvider == null)
            {
                _logger.Warning("Storage provider not available for folder selection");
                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Custom Scan Folder",
                AllowMultiple = false
            };

            // Set suggested start location if current folder exists
            if (!string.IsNullOrWhiteSpace(SelectedScanFolder) && Directory.Exists(SelectedScanFolder))
            {
                var currentFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(SelectedScanFolder);
                if (currentFolder != null)
                {
                    options.SuggestedStartLocation = currentFolder;
                }
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            
            if (folders.Count > 0)
            {
                var selectedPath = folders[0].Path.LocalPath;
                
                // Validate the selected path
                if (ValidateFolder(selectedPath, "custom scan"))
                {
                    SelectedScanFolder = selectedPath;
                    _logger.Information("Selected custom scan folder: {Path}", selectedPath);
                    
                    // TODO: Save to settings
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error selecting custom scan folder");
        }
    }
    
    private async Task SelectIniFolder()
    {
        _logger.Information("Opening INI folder selection dialog");
        
        try
        {
            var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (topLevel?.StorageProvider == null)
            {
                _logger.Warning("Storage provider not available for folder selection");
                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Game INI Files Folder",
                AllowMultiple = false
            };

            // Try to suggest Documents folder as starting location for INI files
            var documentsPath = CrossPlatformHelper.GetDocumentsDirectory();
            if (Directory.Exists(documentsPath))
            {
                var documentsFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(documentsPath);
                if (documentsFolder != null)
                {
                    options.SuggestedStartLocation = documentsFolder;
                }
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            
            if (folders.Count > 0)
            {
                var selectedPath = folders[0].Path.LocalPath;
                
                // Validate the selected path contains game INI files
                if (ValidateIniFolder(selectedPath))
                {
                    _logger.Information("Selected INI folder: {Path}", selectedPath);
                    
                    // TODO: Update game INI path in settings and save
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error selecting INI folder");
        }
    }
    
    private void OpenSettings()
    {
        _logger.Information("Opening settings");
        // TODO: Implement settings file opening
    }
    
    private void OpenCrashLogsFolder()
    {
        _logger.Information("Opening crash logs folder");
        // TODO: Implement folder opening
    }
    
    private async Task CheckForUpdates()
    {
        _logger.Information("Starting manual update check from source: {UpdateSource}", UpdateSource);
        
        try
        {
            _progressService.StartProgress("Checking for updates...");
            
            // Check for updates using the update service
            var updateResult = await _updateService.CheckForUpdatesAsync();
            
            if (updateResult.IsSuccess)
            {
                if (updateResult.IsUpdateAvailable)
                {
                    var message = $"Update available!\n\n" +
                                  $"Current version: {updateResult.CurrentVersion}\n" +
                                  $"Latest version: {updateResult.LatestVersion}\n" +
                                  $"Source: {updateResult.UpdateSource}";
                    
                    _logger.Information("Update available - Current: {Current}, Latest: {Latest}", 
                        updateResult.CurrentVersion, updateResult.LatestVersion);
                    
                    await _notificationService.ShowUpdateAvailableAsync(
                        updateResult.CurrentVersion?.ToString() ?? "Unknown",
                        updateResult.LatestVersion?.ToString() ?? "Unknown",
                        updateResult.LatestRelease?.HtmlUrl ?? "");
                }
                else
                {
                    var message = $"You have the latest version!\n\n" +
                                  $"Current version: {updateResult.CurrentVersion}\n" +
                                  $"Source: {updateResult.UpdateSource}";
                    
                    _logger.Information("No update available - Current version: {Current}", updateResult.CurrentVersion);
                    
                    await _notificationService.ShowNoUpdateAvailableAsync(
                        updateResult.CurrentVersion?.ToString() ?? "Unknown",
                        updateResult.UpdateSource);
                }
            }
            else
            {
                _logger.Warning("Update check failed: {Error}", updateResult.ErrorMessage);
                
                await _notificationService.ShowUpdateCheckErrorAsync(
                    updateResult.ErrorMessage ?? "Unknown error occurred during update check");
            }
            
            _progressService.CompleteProgress("Update check completed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during manual update check");
            _progressService.FailProgress($"Update check failed: {ex.Message}");
            
            await _notificationService.ShowUpdateCheckErrorAsync(
                $"Update check failed: {ex.Message}");
        }
    }
    
    private async void ShowAbout()
    {
        _logger.Information("Showing about dialog");
        
        try
        {
            var aboutDialog = new Views.AboutDialog
            {
                DataContext = new AboutDialogViewModel()
            };
            
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                await aboutDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show about dialog");
        }
    }
    
    private async void ShowHelp()
    {
        _logger.Information("Showing help dialog");
        
        try
        {
            var helpDialog = new Views.HelpDialog
            {
                DataContext = new HelpDialogViewModel()
            };
            
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                await helpDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show help dialog");
        }
    }
    
    private void Exit()
    {
        _logger.Information("Exit command triggered");
        
        // Use platform-independent exit method
        CrossPlatformHelper.ExitApplication(0);
    }
    
    private async Task ManageGameFiles(string category, string action)
    {
        _logger.Information("Managing game files: {Action} {Category}", action, category);
        
        try
        {
            GameFileOperationResult result = action.ToUpperInvariant() switch
            {
                "BACKUP" => await _gameFileManager.BackupFilesAsync(category),
                "RESTORE" => await _gameFileManager.RestoreFilesAsync(category),
                "REMOVE" => await _gameFileManager.RemoveFilesAsync(category),
                _ => new GameFileOperationResult { Success = false, Message = $"Unknown action: {action}" }
            };

            if (result.Success)
            {
                _logger.Information("Game file operation completed successfully: {Message}", result.Message);
            }
            else
            {
                _logger.Warning("Game file operation failed: {Message}", result.Message);
            }

            // Show result to user via notification
            await _notificationService.ShowGameFileOperationAsync(action, category, result);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during game file operation: {Action} {Category}", action, category);
            var errorResult = new GameFileOperationResult 
            { 
                Success = false, 
                Message = $"Operation failed: {ex.Message}" 
            };
            await _notificationService.ShowGameFileOperationAsync(action, category, errorResult);
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void InitializeResourceLinks()
    {
        foreach (var link in new[]
        {
            new ResourceLinkViewModel("BUFFOUT 4 INSTALLATION", "https://www.nexusmods.com/fallout4/articles/3115"),
            new ResourceLinkViewModel("FALLOUT 4 SETUP TIPS", "https://www.nexusmods.com/fallout4/articles/4141"),
            new ResourceLinkViewModel("IMPORTANT PATCHES LIST", "https://www.nexusmods.com/fallout4/articles/3769"),
            new ResourceLinkViewModel("BUFFOUT 4 NEXUS", "https://www.nexusmods.com/fallout4/mods/47359"),
            new ResourceLinkViewModel("CLASSIC NEXUS", "https://www.nexusmods.com/fallout4/mods/56255"),
            new ResourceLinkViewModel("CLASSIC GITHUB", "https://github.com/evildarkarchon/CLASSIC-Fallout4"),
            new ResourceLinkViewModel("DDS TEXTURE SCANNER", "https://www.nexusmods.com/fallout4/mods/71588"),
            new ResourceLinkViewModel("BETHINI PIE", "https://www.nexusmods.com/site/mods/631"),
            new ResourceLinkViewModel("WRYE BASH", "https://www.nexusmods.com/fallout4/mods/20032")
        })
        {
            ResourceLinks.Add(link);
        }
    }
    
    private void LoadSettings()
    {
        _logger.Information("Loading settings from YAML");
        
        var settings = _settingsService.Settings;
        
        // Load UI properties from settings
        SelectedModsFolder = settings.StagingModsPath ?? string.Empty;
        SelectedScanFolder = settings.CustomScanPath ?? string.Empty;
        FcxMode = settings.FCXMode;
        SimplifyLogs = settings.SimplifyLogs;
        UpdateCheck = settings.UpdateCheck;
        VrMode = settings.VRMode;
        AudioNotifications = settings.SoundOnCompletion;
        UpdateSource = settings.UpdateSource;
        
        // Note: ShowFormIdValues, MoveInvalidLogs are UI-only settings not persisted
    }
    
    private async Task SaveSettings()
    {
        _logger.Information("Saving settings to YAML");
        
        var settings = _settingsService.Settings;
        
        // Update settings object
        settings.StagingModsPath = SelectedModsFolder;
        settings.CustomScanPath = SelectedScanFolder;
        settings.FCXMode = FcxMode;
        settings.SimplifyLogs = SimplifyLogs;
        settings.UpdateCheck = UpdateCheck;
        settings.VRMode = VrMode;
        settings.SoundOnCompletion = AudioNotifications;
        settings.UpdateSource = UpdateSource;
        
        // Save to disk
        await _settingsService.SaveAsync();
    }
    
    /// <summary>
    /// Validates that a selected folder is accessible and appropriate for its intended use
    /// </summary>
    private bool ValidateFolder(string folderPath, string folderType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _logger.Warning("Empty folder path provided for {FolderType}", folderType);
                return false;
            }

            if (!Directory.Exists(folderPath))
            {
                _logger.Warning("Selected {FolderType} folder does not exist: {Path}", folderType, folderPath);
                return false;
            }

            // Test read access using cross-platform helper
            if (!CrossPlatformHelper.TryAccessDirectory(folderPath))
            {
                _logger.Warning("Cannot access {FolderType} folder: {Path}", folderType, folderPath);
                return false;
            }
            
            _logger.Information("Successfully validated {FolderType} folder: {Path}", folderType, folderPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error(ex, "Access denied to {FolderType} folder: {Path}", folderType, folderPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating {FolderType} folder: {Path}", folderType, folderPath);
            return false;
        }
    }
    
    /// <summary>
    /// Validates that a selected folder contains game INI files
    /// </summary>
    private bool ValidateIniFolder(string folderPath)
    {
        try
        {
            if (!ValidateFolder(folderPath, "INI"))
                return false;

            // Look for common game INI files
            var commonIniFiles = new[] 
            {
                "Fallout4.ini", "Fallout4Prefs.ini", "Fallout4Custom.ini",
                "Skyrim.ini", "SkyrimPrefs.ini", "SkyrimCustom.ini",
                "SkyrimVR.ini", "SkyrimVRPrefs.ini"
            };

            var foundIniFiles = commonIniFiles.Where(iniFile => 
                File.Exists(Path.Combine(folderPath, iniFile))).ToList();

            if (foundIniFiles.Any())
            {
                _logger.Information("Found game INI files in folder: {Files}", string.Join(", ", foundIniFiles));
                return true;
            }
            else
            {
                _logger.Warning("No recognized game INI files found in selected folder: {Path}", folderPath);
                _logger.Information("Expected files: {ExpectedFiles}", string.Join(", ", commonIniFiles));
                
                // Still return true as user might have custom INI setup
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating INI folder: {Path}", folderPath);
            return false;
        }
    }
    
    private void OnProgressUpdated(object? sender, ProgressUpdateEventArgs e)
    {
        var state = e.State;
        
        ProgressPercentage = state.Percentage;
        ProgressMessage = state.CurrentOperation;
        ProgressDetails = state.Details ?? string.Empty;
        IsProgressIndeterminate = state.IsIndeterminate;
        EstimatedTimeRemaining = state.EstimatedTimeRemaining;
    }
    
    private void UpdateScanStatistics(ScanResult result)
    {
        if (result == null)
        {
            ScanStatistics = string.Empty;
            return;
        }
        
        var stats = new System.Text.StringBuilder();
        stats.AppendLine($"Last Scan: {result.EndTime:yyyy-MM-dd HH:mm:ss}");
        stats.AppendLine($"Total Logs: {result.TotalLogs}");
        stats.AppendLine($"Successful: {result.SuccessfulScans} ({result.SuccessRate:F1}%)");
        stats.AppendLine($"Failed: {result.FailedScans} ({result.FailureRate:F1}%)");
        stats.AppendLine($"Processing Time: {result.ProcessingTime:mm\\:ss}");
        
        if (result.ModConflicts.Any())
        {
            stats.AppendLine("Top Conflicts:");
            foreach (var conflict in result.ModConflicts.Take(3))
            {
                stats.AppendLine($"  • {conflict.Key} ({conflict.Value}x)");
            }
        }
        
        ScanStatistics = stats.ToString();
    }
    
    #endregion
}

public class ResourceLinkViewModel
{
    public ResourceLinkViewModel(string title, string url)
    {
        Title = title;
        Url = url;
    }
    
    public string Title { get; }
    public string Url { get; }
}
