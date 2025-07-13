using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for reporting progress during long-running operations
/// </summary>
public class ProgressService : IProgressService
{
    private readonly ILogger _logger;
    private ProgressState _currentState = new();
    private readonly object _lockObject = new();

    public ProgressService(ILogger logger)
    {
        _logger = logger;
    }

    public event EventHandler<ProgressUpdateEventArgs>? ProgressUpdated;

    public ProgressState CurrentState
    {
        get
        {
            lock (_lockObject)
            {
                return new ProgressState
                {
                    OperationName = _currentState.OperationName,
                    TotalItems = _currentState.TotalItems,
                    CurrentItem = _currentState.CurrentItem,
                    Percentage = _currentState.Percentage,
                    CurrentOperation = _currentState.CurrentOperation,
                    Details = _currentState.Details,
                    IsIndeterminate = _currentState.IsIndeterminate,
                    IsActive = _currentState.IsActive,
                    StartTime = _currentState.StartTime,
                    EstimatedTimeRemaining = _currentState.EstimatedTimeRemaining,
                    ErrorMessage = _currentState.ErrorMessage,
                    IsCompleted = _currentState.IsCompleted,
                    HasError = _currentState.HasError
                };
            }
        }
    }

    public void StartProgress(string operationName, int totalItems = 0)
    {
        lock (_lockObject)
        {
            _currentState = new ProgressState
            {
                OperationName = operationName,
                TotalItems = totalItems,
                CurrentItem = 0,
                Percentage = 0,
                CurrentOperation = "Starting...",
                IsIndeterminate = totalItems == 0,
                IsActive = true,
                StartTime = DateTime.Now,
                IsCompleted = false,
                HasError = false
            };
        }

        _logger.Information("Progress started: {OperationName} with {TotalItems} items", operationName, totalItems);
        FireProgressUpdated();
    }

    public void UpdateProgress(int currentItem, string currentOperation, string? details = null)
    {
        lock (_lockObject)
        {
            if (!_currentState.IsActive) return;

            _currentState.CurrentItem = currentItem;
            _currentState.CurrentOperation = currentOperation;
            _currentState.Details = details;

            if (_currentState.TotalItems > 0)
            {
                _currentState.Percentage = Math.Min(100, (int)((double)currentItem / _currentState.TotalItems * 100));
                _currentState.IsIndeterminate = false;

                // Calculate estimated time remaining
                var elapsed = DateTime.Now - _currentState.StartTime;
                if (currentItem > 0 && elapsed.TotalSeconds > 1)
                {
                    var averageTimePerItem = elapsed.TotalSeconds / currentItem;
                    var remainingItems = _currentState.TotalItems - currentItem;
                    _currentState.EstimatedTimeRemaining = TimeSpan.FromSeconds(averageTimePerItem * remainingItems);
                }
            }
            else
            {
                _currentState.IsIndeterminate = true;
            }
        }

        FireProgressUpdated();
    }

    public void ReportProgress(int percentage, string message, string? details = null)
    {
        lock (_lockObject)
        {
            if (!_currentState.IsActive) return;

            _currentState.Percentage = Math.Clamp(percentage, 0, 100);
            _currentState.CurrentOperation = message;
            _currentState.Details = details;
            _currentState.IsIndeterminate = false;
        }

        FireProgressUpdated();
    }

    public void CompleteProgress(string completionMessage)
    {
        lock (_lockObject)
        {
            _currentState.IsActive = false;
            _currentState.IsCompleted = true;
            _currentState.Percentage = 100;
            _currentState.CurrentOperation = completionMessage;
            _currentState.HasError = false;
        }

        _logger.Information("Progress completed: {CompletionMessage}", completionMessage);
        FireProgressUpdated();
    }

    public void FailProgress(string errorMessage)
    {
        lock (_lockObject)
        {
            _currentState.IsActive = false;
            _currentState.IsCompleted = true;
            _currentState.HasError = true;
            _currentState.ErrorMessage = errorMessage;
            _currentState.CurrentOperation = "Failed";
        }

        _logger.Error("Progress failed: {ErrorMessage}", errorMessage);
        FireProgressUpdated();
    }

    private void FireProgressUpdated()
    {
        var args = new ProgressUpdateEventArgs { State = CurrentState };
        ProgressUpdated?.Invoke(this, args);
    }
}
