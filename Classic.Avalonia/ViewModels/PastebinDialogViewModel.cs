using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Avalonia.ViewModels;

public class PastebinDialogViewModel : ViewModelBase
{
    private readonly IPastebinService _pastebinService;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;

    private string _urlOrId = string.Empty;
    private bool _isFetching;
    private string _statusMessage = "Enter a Pastebin URL or ID to fetch a crash log";

    public PastebinDialogViewModel(IPastebinService pastebinService, INotificationService notificationService,
        ILogger logger)
    {
        _pastebinService = pastebinService ?? throw new ArgumentNullException(nameof(pastebinService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Commands
        FetchLogCommand = ReactiveCommand.CreateFromTask(FetchLog,
            this.WhenAnyValue(x => x.UrlOrId, x => x.IsFetching,
                (url, fetching) => !string.IsNullOrWhiteSpace(url) && !fetching));

        ClearCommand = ReactiveCommand.Create(Clear);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    #region Properties

    public string UrlOrId
    {
        get => _urlOrId;
        set
        {
            this.RaiseAndSetIfChanged(ref _urlOrId, value);
            ValidateInput();
        }
    }

    public bool IsFetching
    {
        get => _isFetching;
        private set => this.RaiseAndSetIfChanged(ref _isFetching, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsInputValid =>
        !string.IsNullOrWhiteSpace(UrlOrId) && _pastebinService.IsValidPastebinReference(UrlOrId);

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> FetchLogCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    #endregion

    #region Events

    public event EventHandler<PastebinResult>? LogFetched;
    public event EventHandler? DialogClosed;

    #endregion

    #region Private Methods

    private async Task FetchLog()
    {
        if (string.IsNullOrWhiteSpace(UrlOrId)) return;

        try
        {
            IsFetching = true;
            StatusMessage = "Fetching log from Pastebin...";

            _logger.Information("Fetching Pastebin log: {UrlOrId}", UrlOrId);

            var result = await _pastebinService.FetchLogAsync(UrlOrId);

            if (result.Success)
            {
                StatusMessage = $"Successfully fetched log ({result.ContentSize:N0} bytes)";

                await _notificationService.ShowNotificationAsync(
                    "Pastebin Fetch Success",
                    $"Log fetched successfully and saved to: {result.FilePath}",
                    NotificationType.Success);

                LogFetched?.Invoke(this, result);

                // Auto-close after success
                await Task.Delay(1500); // Show success message briefly
                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = $"Failed to fetch log: {result.ErrorMessage}";

                await _notificationService.ShowNotificationAsync(
                    "Pastebin Fetch Failed",
                    result.ErrorMessage ?? "Unknown error occurred",
                    NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while fetching Pastebin log");
            StatusMessage = $"Unexpected error: {ex.Message}";

            await _notificationService.ShowNotificationAsync(
                "Pastebin Fetch Error",
                $"Unexpected error: {ex.Message}",
                NotificationType.Error);
        }
        finally
        {
            IsFetching = false;
        }
    }

    private void Clear()
    {
        UrlOrId = string.Empty;
        StatusMessage = "Enter a Pastebin URL or ID to fetch a crash log";
    }

    private void Cancel()
    {
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }

    private void ValidateInput()
    {
        this.RaisePropertyChanged(nameof(IsInputValid));

        if (string.IsNullOrWhiteSpace(UrlOrId))
            StatusMessage = "Enter a Pastebin URL or ID to fetch a crash log";
        else if (!_pastebinService.IsValidPastebinReference(UrlOrId))
            StatusMessage = "Invalid Pastebin URL or ID format";
        else
            StatusMessage = "Ready to fetch log";
    }

    #endregion
}
