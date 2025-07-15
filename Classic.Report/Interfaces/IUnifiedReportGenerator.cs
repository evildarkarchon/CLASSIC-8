using Classic.Core.Models;
using Classic.Report.Models;

namespace Classic.Report.Interfaces;

/// <summary>
/// Enhanced report generator interface that supports the three-tier template system.
/// This extends the core IReportGenerator to support the new unified architecture.
/// </summary>
public interface IUnifiedReportGenerator
{
    /// <summary>
    /// Generates a report using the unified three-tier system.
    /// </summary>
    /// <param name="analysisResult">The crash log analysis result.</param>
    /// <param name="options">The report generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The generated report as a string.</returns>
    Task<string> GenerateReportAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates only the report sections without applying formatting.
    /// </summary>
    /// <param name="analysisResult">The crash log analysis result.</param>
    /// <param name="options">The report generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The generated report sections.</returns>
    Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies template formatting to the provided sections.
    /// </summary>
    /// <param name="sections">The report sections to format.</param>
    /// <param name="format">The desired report format.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The formatted report as a string.</returns>
    Task<string> ApplyTemplateAsync(
        ReportSections sections,
        ReportTemplateType format,
        CancellationToken cancellationToken = default);
}
