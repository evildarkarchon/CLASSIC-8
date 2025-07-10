namespace Classic.Core.Interfaces;

public interface IScanOrchestrator
{
    Task<object> ExecuteScanAsync(object request, CancellationToken cancellationToken = default);
    Task<object> ScanSingleLogAsync(string logPath, CancellationToken cancellationToken = default);
    Task<object> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
