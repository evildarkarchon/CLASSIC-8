using Classic.Core.Models;
using Classic.Report.Generators;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Strategies;

/// <summary>
/// Enhanced report strategy with advanced formatting and game hints (no FCX required).
/// </summary>
public class EnhancedReportStrategy : ReportStrategyBase
{
    private readonly IExecutiveSummarySectionGenerator? _executiveSummaryGenerator;
    private readonly IPerformanceMetricsSectionGenerator? _performanceGenerator;
    private readonly IGameHintsSectionGenerator? _gameHintsGenerator;

    public override string Name => "Enhanced Report";
    public override ReportTemplateType Format => ReportTemplateType.Enhanced;
    public override bool RequiresFCXMode => false;

    public EnhancedReportStrategy(
        ILogger logger,
        IHeaderSectionGenerator headerGenerator,
        IErrorSectionGenerator errorGenerator,
        ISuspectSectionGenerator suspectGenerator,
        ISettingsValidationSectionGenerator settingsGenerator,
        IPluginSectionGenerator pluginGenerator,
        IFormIdSectionGenerator formIdGenerator,
        INamedRecordSectionGenerator namedRecordGenerator,
        IFooterSectionGenerator footerGenerator,
        IExecutiveSummarySectionGenerator? executiveSummaryGenerator = null,
        IPerformanceMetricsSectionGenerator? performanceGenerator = null,
        IGameHintsSectionGenerator? gameHintsGenerator = null)
        : base(logger, headerGenerator, errorGenerator, suspectGenerator, settingsGenerator,
            pluginGenerator, formIdGenerator, namedRecordGenerator, footerGenerator)
    {
        _executiveSummaryGenerator = executiveSummaryGenerator;
        _performanceGenerator = performanceGenerator;
        _gameHintsGenerator = gameHintsGenerator;
    }

    protected override async Task GenerateSpecificSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        _logger.Debug("Enhanced report strategy: generating enhanced sections");

        // Generate executive summary if generator is available
        if (_executiveSummaryGenerator != null)
        {
            sections.ExecutiveSummary = await _executiveSummaryGenerator
                .GenerateAsync(analysisResult, options, cancellationToken)
                .ConfigureAwait(false);
        }

        // Generate performance metrics if enabled and generator is available
        if (options.IncludePerformanceMetrics && _performanceGenerator != null)
        {
            sections.Performance = await _performanceGenerator
                .GenerateAsync(analysisResult, options, cancellationToken)
                .ConfigureAwait(false);
        }

        // Generate game hints if enabled and generator is available
        if (options.IncludeGameHints && _gameHintsGenerator != null)
        {
            sections.GameHints = await _gameHintsGenerator
                .GenerateAsync(analysisResult, options, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.Debug("Enhanced report strategy: completed enhanced sections generation");
    }
}
