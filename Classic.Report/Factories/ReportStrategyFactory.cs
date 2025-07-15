using Classic.Report.Interfaces;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Factories;

/// <summary>
/// Factory for creating and managing report strategy instances.
/// </summary>
public class ReportStrategyFactory : IReportStrategyFactory
{
    private readonly Dictionary<ReportTemplateType, IReportStrategy> _strategies;
    private readonly ILogger _logger;

    public ReportStrategyFactory(
        IEnumerable<IReportStrategy> strategies,
        ILogger logger)
    {
        _logger = logger;
        _strategies = new Dictionary<ReportTemplateType, IReportStrategy>();

        foreach (var strategy in strategies)
        {
            RegisterStrategy(strategy);
        }

        _logger.Information("Initialized ReportStrategyFactory with {StrategyCount} strategies",
            _strategies.Count);
    }

    public IReportStrategy GetStrategy(ReportTemplateType format)
    {
        if (_strategies.TryGetValue(format, out var strategy))
        {
            _logger.Debug("Retrieved {StrategyName} for format {Format}", strategy.Name, format);
            return strategy;
        }

        _logger.Warning("No strategy found for format {Format}, falling back to Standard", format);

        // Fallback to Standard if available, otherwise throw
        if (_strategies.TryGetValue(ReportTemplateType.Standard, out var fallbackStrategy))
        {
            return fallbackStrategy;
        }

        throw new InvalidOperationException(
            $"No strategy registered for format '{format}' and no Standard fallback available.");
    }

    public IEnumerable<IReportStrategy> GetAllStrategies()
    {
        return _strategies.Values;
    }

    public void RegisterStrategy(IReportStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        if (_strategies.ContainsKey(strategy.Format))
        {
            _logger.Warning("Replacing existing strategy for format {Format}: {OldStrategy} -> {NewStrategy}",
                strategy.Format, _strategies[strategy.Format].Name, strategy.Name);
        }

        _strategies[strategy.Format] = strategy;
        _logger.Debug("Registered strategy {StrategyName} for format {Format}",
            strategy.Name, strategy.Format);
    }
}
