using Classic.Core.Models;
using Classic.Report.Models;

namespace Classic.Report.Interfaces;

/// <summary>
/// Defines the strategy pattern for different report generation approaches.
/// </summary>
public interface IReportStrategy
{
    /// <summary>
    /// Gets the name of this report strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the report format this strategy generates.
    /// </summary>
    ReportTemplateType Format { get; }

    /// <summary>
    /// Gets whether this strategy requires FCX mode to be enabled.
    /// </summary>
    bool RequiresFCXMode { get; }

    /// <summary>
    /// Generates report sections from the crash log analysis result.
    /// </summary>
    /// <param name="analysisResult">The crash log analysis result.</param>
    /// <param name="options">The report generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The generated report sections.</returns>
    Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}
