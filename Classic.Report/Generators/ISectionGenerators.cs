using Classic.Report.Interfaces;
using Classic.Report.Models;

namespace Classic.Report.Generators;

/// <summary>
/// Interface for generating header sections.
/// </summary>
public interface IHeaderSectionGenerator : ISectionGenerator<HeaderSection>
{
}

/// <summary>
/// Interface for generating error sections.
/// </summary>
public interface IErrorSectionGenerator : ISectionGenerator<ErrorSection>
{
}

/// <summary>
/// Interface for generating suspect sections.
/// </summary>
public interface ISuspectSectionGenerator
{
    /// <summary>
    /// Generates suspect sections from analysis result.
    /// </summary>
    Task<List<SuspectSection>> GenerateAsync(
        Classic.Core.Models.CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating settings validation sections.
/// </summary>
public interface ISettingsValidationSectionGenerator : ISectionGenerator<SettingsValidationSection>
{
}

/// <summary>
/// Interface for generating plugin sections.
/// </summary>
public interface IPluginSectionGenerator
{
    /// <summary>
    /// Generates plugin sections from analysis result.
    /// </summary>
    Task<List<PluginSection>> GenerateAsync(
        Classic.Core.Models.CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating FormID sections.
/// </summary>
public interface IFormIdSectionGenerator
{
    /// <summary>
    /// Generates FormID sections from analysis result.
    /// </summary>
    Task<List<FormIdSection>> GenerateAsync(
        Classic.Core.Models.CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating named record sections.
/// </summary>
public interface INamedRecordSectionGenerator
{
    /// <summary>
    /// Generates named record sections from analysis result.
    /// </summary>
    Task<List<NamedRecordSection>> GenerateAsync(
        Classic.Core.Models.CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating footer sections.
/// </summary>
public interface IFooterSectionGenerator : ISectionGenerator<FooterSection>
{
}

/// <summary>
/// Interface for generating executive summary sections.
/// </summary>
public interface IExecutiveSummarySectionGenerator : ISectionGenerator<ExecutiveSummarySection>
{
}

/// <summary>
/// Interface for generating performance metrics sections.
/// </summary>
public interface IPerformanceMetricsSectionGenerator : ISectionGenerator<PerformanceMetricsSection>
{
}

/// <summary>
/// Interface for generating game hints sections.
/// </summary>
public interface IGameHintsSectionGenerator : ISectionGenerator<GameHintsSection>
{
}
