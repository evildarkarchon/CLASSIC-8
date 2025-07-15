using Classic.Core.Models;
using Classic.Report.Generators;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Strategies;

/// <summary>
/// Standard report strategy for basic analysis with minimal formatting.
/// </summary>
public class StandardReportStrategy : ReportStrategyBase
{
    public override string Name => "Standard Report";
    public override ReportTemplateType Format => ReportTemplateType.Standard;
    public override bool RequiresFCXMode => false;

    public StandardReportStrategy(
        ILogger logger,
        IHeaderSectionGenerator headerGenerator,
        IErrorSectionGenerator errorGenerator,
        ISuspectSectionGenerator suspectGenerator,
        ISettingsValidationSectionGenerator settingsGenerator,
        IPluginSectionGenerator pluginGenerator,
        IFormIdSectionGenerator formIdGenerator,
        INamedRecordSectionGenerator namedRecordGenerator,
        IFooterSectionGenerator footerGenerator)
        : base(logger, headerGenerator, errorGenerator, suspectGenerator, settingsGenerator,
            pluginGenerator, formIdGenerator, namedRecordGenerator, footerGenerator)
    {
    }

    protected override async Task GenerateSpecificSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        // Standard report only includes common sections
        // No additional sections needed
        await Task.CompletedTask.ConfigureAwait(false);

        _logger.Debug("Standard report strategy: using common sections only");
    }
}
