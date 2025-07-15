using Classic.Core.Models;
using Classic.Report.Models;

namespace Classic.Report.Interfaces;

/// <summary>
/// Interface for generating individual report sections.
/// </summary>
/// <typeparam name="TSection">The type of section this generator creates.</typeparam>
public interface ISectionGenerator<TSection> where TSection : ReportSectionBase
{
    /// <summary>
    /// Generates a report section from the analysis result.
    /// </summary>
    /// <param name="analysisResult">The crash log analysis result.</param>
    /// <param name="options">The report generation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The generated section.</returns>
    Task<TSection> GenerateAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}
