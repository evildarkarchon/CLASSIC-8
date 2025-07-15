using Classic.Core.Models;
using Classic.Report.Interfaces;
using Classic.Report.Models;
using Classic.Report.Generators;
using Serilog;

namespace Classic.Report.Strategies;

/// <summary>
/// Base class for report strategies providing common functionality.
/// </summary>
public abstract class ReportStrategyBase : IReportStrategy
{
    protected readonly ILogger _logger;
    protected readonly IHeaderSectionGenerator _headerGenerator;
    protected readonly IErrorSectionGenerator _errorGenerator;
    protected readonly ISuspectSectionGenerator _suspectGenerator;
    protected readonly ISettingsValidationSectionGenerator _settingsGenerator;
    protected readonly IPluginSectionGenerator _pluginGenerator;
    protected readonly IFormIdSectionGenerator _formIdGenerator;
    protected readonly INamedRecordSectionGenerator _namedRecordGenerator;
    protected readonly IFooterSectionGenerator _footerGenerator;

    protected ReportStrategyBase(
        ILogger logger,
        IHeaderSectionGenerator headerGenerator,
        IErrorSectionGenerator errorGenerator,
        ISuspectSectionGenerator suspectGenerator,
        ISettingsValidationSectionGenerator settingsGenerator,
        IPluginSectionGenerator pluginGenerator,
        IFormIdSectionGenerator formIdGenerator,
        INamedRecordSectionGenerator namedRecordGenerator,
        IFooterSectionGenerator footerGenerator)
    {
        _logger = logger;
        _headerGenerator = headerGenerator;
        _errorGenerator = errorGenerator;
        _suspectGenerator = suspectGenerator;
        _settingsGenerator = settingsGenerator;
        _pluginGenerator = pluginGenerator;
        _formIdGenerator = formIdGenerator;
        _namedRecordGenerator = namedRecordGenerator;
        _footerGenerator = footerGenerator;
    }

    public abstract string Name { get; }
    public abstract ReportTemplateType Format { get; }
    public abstract bool RequiresFCXMode { get; }

    public virtual async Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating {StrategyName} report sections", Name);

        var sections = new ReportSections();

        try
        {
            // Generate common sections for all strategies
            await GenerateCommonSectionsAsync(sections, analysisResult, options, cancellationToken)
                .ConfigureAwait(false);

            // Generate strategy-specific sections
            await GenerateSpecificSectionsAsync(sections, analysisResult, options, cancellationToken)
                .ConfigureAwait(false);

            _logger.Information("Successfully generated {StrategyName} report sections", Name);
            return sections;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate {StrategyName} report sections", Name);
            throw;
        }
    }

    /// <summary>
    /// Generates common sections that are included in all report formats.
    /// </summary>
    protected virtual async Task GenerateCommonSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        // Generate basic sections
        sections.Header = await _headerGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.MainError = await _errorGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.CrashSuspects = await _suspectGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.Settings = await _settingsGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.PluginSuspects = await _pluginGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.FormIdSuspects = await _formIdGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.NamedRecords = await _namedRecordGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
        sections.Footer = await _footerGenerator.GenerateAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Generates strategy-specific sections. Override in derived classes.
    /// </summary>
    protected virtual Task GenerateSpecificSectionsAsync(
        ReportSections sections,
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        // Default implementation does nothing
        return Task.CompletedTask;
    }
}
