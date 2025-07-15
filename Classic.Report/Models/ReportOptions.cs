namespace Classic.Report.Models;

/// <summary>
/// Configuration options for report generation.
/// </summary>
public class ReportOptions
{
    /// <summary>
    /// Gets or sets whether FCX mode is enabled.
    /// </summary>
    public bool FCXMode { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use enhanced formatting.
    /// </summary>
    public bool UseEnhancedFormatting { get; set; } = false;

    /// <summary>
    /// Gets or sets the preferred report format.
    /// </summary>
    public ReportTemplateType PreferredFormat { get; set; } = ReportTemplateType.Standard;

    /// <summary>
    /// Gets or sets whether to automatically select the best format based on analysis complexity.
    /// </summary>
    public bool AutoSelectFormat { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include performance metrics in the report.
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include game hints in the report.
    /// </summary>
    public bool IncludeGameHints { get; set; } = true;

    /// <summary>
    /// Gets or sets the output file path for the report.
    /// </summary>
    public string? OutputPath { get; set; }
}
