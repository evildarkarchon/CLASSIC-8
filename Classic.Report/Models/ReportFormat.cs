namespace Classic.Report.Models;

/// <summary>
/// Defines the available report template types for crash log analysis.
/// </summary>
public enum ReportTemplateType
{
    /// <summary>
    /// Basic format for quick analysis with minimal formatting.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Advanced formatting with game hints but no FCX file checks.
    /// </summary>
    Enhanced = 1,

    /// <summary>
    /// Full FCX mode with file system checks and extended metrics.
    /// </summary>
    Advanced = 2
}
