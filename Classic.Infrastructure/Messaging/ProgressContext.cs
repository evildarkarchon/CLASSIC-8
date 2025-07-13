using Classic.Core.Interfaces;

namespace Classic.Infrastructure.Messaging;

/// <summary>
/// Shared progress context implementation that works with any IMessageHandler
/// </summary>
public class ProgressContext : IDisposable
{
    private readonly IMessageHandler _handler;
    private readonly string _operation;
    private readonly int _total;
    private int _current;
    private bool _disposed;

    public ProgressContext(IMessageHandler handler, string operation, int total)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        _total = total;
        _current = 0;

        // Report initial progress
        _handler.ReportProgress(_operation, _current, _total);
    }

    /// <summary>
    /// Gets the current progress count
    /// </summary>
    public int Current => _current;

    /// <summary>
    /// Gets the total progress count
    /// </summary>
    public int Total => _total;

    /// <summary>
    /// Gets the operation name
    /// </summary>
    public string Operation => _operation;

    /// <summary>
    /// Gets the current progress percentage
    /// </summary>
    public double Percentage => _total > 0 ? (double)_current / _total * 100 : 0;

    /// <summary>
    /// Gets whether the operation is completed
    /// </summary>
    public bool IsCompleted => _current >= _total;

    /// <summary>
    /// Increments the progress by one step
    /// </summary>
    public void Increment()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _current++;
        _handler.ReportProgress(_operation, _current, _total);
    }

    /// <summary>
    /// Sets the progress to a specific value
    /// </summary>
    /// <param name="current">The current progress value</param>
    public void SetProgress(int current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _current = Math.Min(current, _total);
        _handler.ReportProgress(_operation, _current, _total);
    }

    /// <summary>
    /// Completes the progress operation
    /// </summary>
    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _current = _total;
        _handler.ReportProgress(_operation, _current, _total);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Ensure progress is completed when disposed
        if (_current < _total)
        {
            _current = _total;
            _handler.ReportProgress(_operation, _current, _total);
        }

        _disposed = true;
    }
}
