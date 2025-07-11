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

namespace Classic.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly ILogger _logger;
    
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
    
    public MainWindowViewModel(IScanOrchestrator scanOrchestrator, ILogger logger)
    {
        _scanOrchestrator = scanOrchestrator;
        _logger = logger;
        
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
        
        // Load settings
        LoadSettings();
    }
    
    #region Properties
    
    public string SelectedModsFolder
    {
        get => _selectedModsFolder;
        set => this.RaiseAndSetIfChanged(ref _selectedModsFolder, value);
    }
    
    public string SelectedScanFolder
    {
        get => _selectedScanFolder;
        set => this.RaiseAndSetIfChanged(ref _selectedScanFolder, value);
    }
    
    public bool FcxMode
    {
        get => _fcxMode;
        set => this.RaiseAndSetIfChanged(ref _fcxMode, value);
    }
    
    public bool SimplifyLogs
    {
        get => _simplifyLogs;
        set => this.RaiseAndSetIfChanged(ref _simplifyLogs, value);
    }
    
    public bool UpdateCheck
    {
        get => _updateCheck;
        set => this.RaiseAndSetIfChanged(ref _updateCheck, value);
    }
    
    public bool VrMode
    {
        get => _vrMode;
        set => this.RaiseAndSetIfChanged(ref _vrMode, value);
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
        set => this.RaiseAndSetIfChanged(ref _audioNotifications, value);
    }
    
    public string UpdateSource
    {
        get => _updateSource;
        set => this.RaiseAndSetIfChanged(ref _updateSource, value);
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
            
            var scanRequest = new ScanRequest
            {
                ModsPath = !string.IsNullOrWhiteSpace(SelectedModsFolder) ? SelectedModsFolder : null,
                EnableFcxMode = FcxMode,
                SimplifyLogs = SimplifyLogs,
                ShowFormIdValues = ShowFormIdValues,
                MoveUnsolvedLogs = MoveInvalidLogs,
                OutputDirectory = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Classic", "CrashLogs")
            };
            
            // Add custom scan folder to log files if specified
            if (!string.IsNullOrWhiteSpace(SelectedScanFolder))
            {
                // TODO: Enumerate crash log files from custom folder
            }
            
            var result = await _scanOrchestrator.ExecuteScanAsync(scanRequest);
            
            _logger.Information("Crash logs scan completed successfully");
            // TODO: Handle result and show notification
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Error during crash logs scan");
            // TODO: Show error notification
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
            
            var scanRequest = new ScanRequest
            {
                ModsPath = !string.IsNullOrWhiteSpace(SelectedModsFolder) ? SelectedModsFolder : null,
                EnableFcxMode = FcxMode,
                OutputDirectory = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Classic", "GameFiles")
            };
            
            var result = await _scanOrchestrator.ExecuteScanAsync(scanRequest);
            
            _logger.Information("Game files scan completed successfully");
            // TODO: Handle result and show notification
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Error during game files scan");
            // TODO: Show error notification
        }
        finally
        {
            IsScanning = false;
        }
    }
    
    private async Task SelectModsFolder()
    {
        // TODO: Implement folder selection dialog
        _logger.Information("Selecting mods folder");
        await Task.CompletedTask;
    }
    
    private async Task SelectScanFolder()
    {
        // TODO: Implement folder selection dialog
        _logger.Information("Selecting scan folder");
        await Task.CompletedTask;
    }
    
    private async Task SelectIniFolder()
    {
        // TODO: Implement folder selection dialog
        _logger.Information("Selecting INI folder");
        await Task.CompletedTask;
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
        _logger.Information("Checking for updates");
        // TODO: Implement update check
        await Task.CompletedTask;
    }
    
    private void ShowAbout()
    {
        _logger.Information("Showing about dialog");
        // TODO: Show about dialog
    }
    
    private void ShowHelp()
    {
        _logger.Information("Showing help");
        // TODO: Show help dialog
    }
    
    private void Exit()
    {
        _logger.Information("Exiting application");
        // TODO: Implement exit logic
    }
    
    private async Task ManageGameFiles(string category, string action)
    {
        _logger.Information("Managing game files: {Action} {Category}", action, category);
        // TODO: Implement game file management
        await Task.CompletedTask;
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
        // TODO: Load settings from configuration
        _logger.Information("Loading settings");
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
