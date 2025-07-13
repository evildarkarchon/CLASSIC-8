using ReactiveUI;
using System;
using System.Reactive;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Avalonia.ViewModels;

public class PapyrusMonitorDialogViewModel : ViewModelBase, IDisposable
{
    private readonly IPapyrusMonitoringService _papyrusService;
    private readonly ILogger _logger;

    private PapyrusStats _currentStats = new()
    {
        Timestamp = DateTime.Now,
        LogFileExists = false
    };

    private string _statusMessage = "Initializing...";
    private bool _isMonitoring;

    public PapyrusMonitorDialogViewModel(IPapyrusMonitoringService papyrusService, ILogger logger)
    {
        _papyrusService = papyrusService ?? throw new ArgumentNullException(nameof(papyrusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Commands
        StopMonitoringCommand = ReactiveCommand.CreateFromTask(StopMonitoring);

        // Subscribe to events
        _papyrusService.StatsUpdated += OnStatsUpdated;
        _papyrusService.MonitoringError += OnMonitoringError;

        // Initialize with current stats
        InitializeAsync();
    }

    #region Properties

    public PapyrusStats CurrentStats
    {
        get => _currentStats;
        private set => this.RaiseAndSetIfChanged(ref _currentStats, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set => this.RaiseAndSetIfChanged(ref _isMonitoring, value);
    }

    public string LastUpdated => CurrentStats.Timestamp.ToString("HH:mm:ss");

    public string DumpsText => CurrentStats.Dumps.ToString();
    public string StacksText => CurrentStats.Stacks.ToString();
    public string RatioText => CurrentStats.Ratio.ToString("F3");
    public string WarningsText => CurrentStats.Warnings.ToString();
    public string ErrorsText => CurrentStats.Errors.ToString();

    // Status indicators
    public string RatioStatus => GetRatioStatus(CurrentStats.Ratio);
    public string RatioStatusColor => GetRatioStatusColor(CurrentStats.Ratio);

    public string WarningsStatus => CurrentStats.Warnings > 0 ? "⚠️" : "✓";
    public string WarningsStatusColor => CurrentStats.Warnings > 0 ? "Orange" : "Green";

    public string ErrorsStatus => CurrentStats.Errors > 0 ? "❌" : "✓";
    public string ErrorsStatusColor => CurrentStats.Errors > 0 ? "Red" : "Green";

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> StopMonitoringCommand { get; }

    #endregion

    #region Private Methods

    private async void InitializeAsync()
    {
        try
        {
            IsMonitoring = _papyrusService.IsMonitoring;

            if (!IsMonitoring)
            {
                // Get initial stats
                var initialStats = await _papyrusService.GetCurrentStatsAsync();
                UpdateStats(initialStats);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize Papyrus monitor dialog");
            StatusMessage = $"Failed to initialize: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task StopMonitoring()
    {
        try
        {
            await _papyrusService.StopMonitoringAsync();
            IsMonitoring = false;
            StatusMessage = "Monitoring stopped";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop Papyrus monitoring");
            StatusMessage = $"Failed to stop monitoring: {ex.Message}";
        }
    }

    private void OnStatsUpdated(object? sender, PapyrusStats stats)
    {
        UpdateStats(stats);
    }

    private void OnMonitoringError(object? sender, string errorMessage)
    {
        StatusMessage = $"Error: {errorMessage}";
        _logger.Warning("Papyrus monitoring error: {ErrorMessage}", errorMessage);
    }

    private void UpdateStats(PapyrusStats stats)
    {
        CurrentStats = stats;

        // Update status message based on stats
        if (!stats.LogFileExists)
            StatusMessage = stats.ErrorMessage ?? "Papyrus log file not found";
        else if (stats.Errors > 0)
            StatusMessage = $"{stats.Errors} errors detected in Papyrus log!";
        else if (stats.Warnings > 0)
            StatusMessage = $"{stats.Warnings} warnings detected in Papyrus log.";
        else if (stats.Ratio > 0.8)
            StatusMessage = "Warning: High dumps-to-stacks ratio detected!";
        else if (stats.Ratio > 0.5)
            StatusMessage = "Caution: Elevated dumps-to-stacks ratio.";
        else
            StatusMessage = "Papyrus log appears normal.";

        // Raise property changed for computed properties
        this.RaisePropertyChanged(nameof(LastUpdated));
        this.RaisePropertyChanged(nameof(DumpsText));
        this.RaisePropertyChanged(nameof(StacksText));
        this.RaisePropertyChanged(nameof(RatioText));
        this.RaisePropertyChanged(nameof(WarningsText));
        this.RaisePropertyChanged(nameof(ErrorsText));
        this.RaisePropertyChanged(nameof(RatioStatus));
        this.RaisePropertyChanged(nameof(RatioStatusColor));
        this.RaisePropertyChanged(nameof(WarningsStatus));
        this.RaisePropertyChanged(nameof(WarningsStatusColor));
        this.RaisePropertyChanged(nameof(ErrorsStatus));
        this.RaisePropertyChanged(nameof(ErrorsStatusColor));
    }

    private static string GetRatioStatus(double ratio)
    {
        return ratio switch
        {
            > 0.8 => "❌",
            > 0.5 => "⚠️",
            _ => "✓"
        };
    }

    private static string GetRatioStatusColor(double ratio)
    {
        return ratio switch
        {
            > 0.8 => "Red",
            > 0.5 => "Orange",
            _ => "Green"
        };
    }

    #endregion

    public void Dispose()
    {
        if (_papyrusService != null)
        {
            _papyrusService.StatsUpdated -= OnStatsUpdated;
            _papyrusService.MonitoringError -= OnMonitoringError;
        }
    }
}
