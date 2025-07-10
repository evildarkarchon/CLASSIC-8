namespace Classic.Core.Models;

public class ScanStatistics
{
    public int TotalCrashLogs { get; set; }
    public int ProcessedLogs { get; set; }
    public int SuccessfulScans { get; set; }
    public int FailedScans { get; set; }
    public int SuspectsFound { get; set; }
    public int FormIDsAnalyzed { get; set; }
    public int PluginsAnalyzed { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public DateTime ScanStartTime { get; set; }
    public DateTime ScanEndTime { get; set; }

    public double SuccessRate => TotalCrashLogs > 0 ? (double)SuccessfulScans / TotalCrashLogs * 100 : 0;
    public double AverageProcessingTimePerLog => ProcessedLogs > 0 ? TotalProcessingTime.TotalMilliseconds / ProcessedLogs : 0;
}
