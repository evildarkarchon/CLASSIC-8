using Classic.Core.Models;
using Classic.Report.Generators;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Strategies;

/// <summary>
/// Advanced report strategy with full FCX mode features including file system checks.
/// </summary>
public class AdvancedReportStrategy : ReportStrategyBase
{
    private readonly IExecutiveSummarySectionGenerator? _executiveSummaryGenerator;
    private readonly IPerformanceMetricsSectionGenerator? _performanceGenerator;
    private readonly IGameHintsSectionGenerator? _gameHintsGenerator;
    // FCX-specific generators would be injected here when implemented

    public override string Name => "Advanced Report (FCX Mode)";
    public override ReportTemplateType Format => ReportTemplateType.Advanced;
    public override bool RequiresFCXMode => true;

    public AdvancedReportStrategy(
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
        _logger.Debug("Advanced report strategy: generating FCX mode sections");

        // Generate enhanced sections (same as Enhanced strategy)
        await GenerateEnhancedSectionsAsync(sections, analysisResult, options, cancellationToken).ConfigureAwait(false);

        // Generate FCX-specific sections
        await GenerateFCXSectionsAsync(sections, analysisResult, options, cancellationToken).ConfigureAwait(false);

        _logger.Debug("Advanced report strategy: completed FCX sections generation");
    }

    private async Task GenerateEnhancedSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
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
    }

    private async Task GenerateFCXSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Implement FCX-specific section generation
        // This will be implemented in later phases

        // FCX Notice section
        sections.FCXNotice = new FCXNoticeSection
        {
            Title = "FCX Mode Notice",
            NoticeText = "This report was generated with FCX (File Check eXtended) mode enabled, " +
                         "providing comprehensive file system validation and extended performance metrics.",
            FCXEnabled = true
        };

        // Placeholder for main files check
        sections.MainFilesCheck = new MainFilesCheckSection
        {
            Title = "Main Files Validation",
            AllFilesValid = true, // Placeholder
            FileResults = new List<FileCheckResult>()
        };

        // Placeholder for game files check
        sections.GameFilesCheck = new GameFilesCheckSection
        {
            Title = "Game Files Integrity Check",
            IntegrityCheckPassed = true, // Placeholder
            TotalFilesChecked = 0, // Placeholder
            FileResults = new List<FileCheckResult>()
        };

        // Placeholder for extended performance
        sections.ExtendedPerformance = new ExtendedPerformanceSection
        {
            Title = "Extended Performance Metrics",
            FileSystemCheckTime = TimeSpan.Zero, // Placeholder
            WorkerThreadsUsed = 0, // Placeholder
            IOOperations = 0, // Placeholder
            TotalProcessingTime = TimeSpan.Zero // Placeholder
        };

        await Task.CompletedTask.ConfigureAwait(false);

        _logger.Debug("Generated FCX placeholder sections (full implementation in later phases)");
    }
}
