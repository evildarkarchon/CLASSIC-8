using Classic.Report.Models;

namespace Classic.Report.Interfaces;

/// <summary>
/// Factory for creating report strategy instances.
/// </summary>
public interface IReportStrategyFactory
{
    /// <summary>
    /// Gets a report strategy for the specified format.
    /// </summary>
    /// <param name="format">The desired report format.</param>
    /// <returns>The appropriate report strategy.</returns>
    IReportStrategy GetStrategy(ReportTemplateType format);

    /// <summary>
    /// Gets all available report strategies.
    /// </summary>
    /// <returns>Collection of all available strategies.</returns>
    IEnumerable<IReportStrategy> GetAllStrategies();

    /// <summary>
    /// Registers a new report strategy.
    /// </summary>
    /// <param name="strategy">The strategy to register.</param>
    void RegisterStrategy(IReportStrategy strategy);
}
