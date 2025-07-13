using System;
using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for monitoring Papyrus logs in real-time.
/// </summary>
public interface IPapyrusMonitoringService
{
    /// <summary>
    /// Event fired when new Papyrus statistics are available.
    /// </summary>
    event EventHandler<PapyrusStats>? StatsUpdated;

    /// <summary>
    /// Event fired when an error occurs during monitoring.
    /// </summary>
    event EventHandler<string>? MonitoringError;

    /// <summary>
    /// Indicates whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the latest Papyrus statistics without starting monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current Papyrus statistics</returns>
    Task<PapyrusStats> GetCurrentStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring the Papyrus log file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the monitoring operation</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring the Papyrus log file.
    /// </summary>
    /// <returns>Task representing the stop operation</returns>
    Task StopMonitoringAsync();
}
