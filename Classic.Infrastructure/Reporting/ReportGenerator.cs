using Classic.Core.Interfaces;
using Classic.Core.Models;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Classic.Infrastructure.Reporting;

public class ReportGenerator : IReportGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly IReportTemplate _template;

    private const string DocumentationUrl =
        "https://docs.google.com/document/d/17FzeIMJ256xE85XdjoPvv_Zi3C5uHeSTQh6wOZugs4c";

    public ReportGenerator(IFileSystem fileSystem, IReportTemplate? template = null)
    {
        _fileSystem = fileSystem;
        _template = template ?? new Templates.MarkdownReportTemplate();
    }

    public async Task GenerateReportAsync(CrashLog crashLog, ScanStatistics statistics, string outputPath,
        CancellationToken cancellationToken = default)
    {
        // Create a simple analysis result from the basic parameters
        var analysisResult = new CrashLogAnalysisResult
        {
            CrashLog = crashLog,
            Statistics = statistics,
            CrashSuspects = crashLog.Suspects
        };

        await GenerateReportAsync(analysisResult, outputPath, cancellationToken);
    }

    public async Task GenerateReportAsync(CrashLogAnalysisResult analysisResult, string outputPath,
        CancellationToken cancellationToken = default)
    {
        var report = new StringBuilder();

        // Generate all report sections
        GenerateHeader(analysisResult, report);
        GenerateErrorSection(analysisResult, report);
        GenerateSuspectSection(analysisResult, report);
        GenerateSettingsSection(analysisResult, report);
        GenerateModCheckSections(analysisResult, report);
        GeneratePluginSuspectSection(analysisResult, report);
        GenerateFormIdSection(analysisResult, report);
        GenerateNamedRecordSection(analysisResult, report);
        GenerateFooter(analysisResult, report);

        await _fileSystem.File.WriteAllTextAsync(outputPath, report.ToString(), cancellationToken);
    }

    private void GenerateHeader(CrashLogAnalysisResult result, StringBuilder report)
    {
        report.AppendLine(_template.FormatHeader(result.CrashLog.FileName, result.ClassicVersion));
    }

    private void GenerateErrorSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine($"Main Error: {result.CrashLog.MainError}");
        report.AppendLine($"Detected {result.CrashGenName} Version: {result.CrashLog.CrashGenVersion}");

        if (result.IsOutdated)
            report.AppendLine(
                $"* ⚠️ WARNING: Your {result.CrashGenName} is outdated! Please update to the latest version. *");
        else
            report.AppendLine($"* You have the latest version of {result.CrashGenName}! *");
        report.AppendLine();
    }

    private void GenerateSuspectSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        report.AppendLine(_template.FormatSectionHeader("CHECKING IF LOG MATCHES ANY KNOWN CRASH SUSPECTS..."));

        if (result.CrashSuspects.Any())
        {
            foreach (var suspect in result.CrashSuspects.OrderByDescending(s => s.SeverityScore))
                GenerateSuspectEntry(suspect, report);

            report.AppendLine();
            report.AppendLine(
                "* FOR DETAILED DESCRIPTIONS AND POSSIBLE SOLUTIONS TO ANY ABOVE DETECTED CRASH SUSPECTS *");
            report.AppendLine($"* SEE: {DocumentationUrl} *");
            report.AppendLine();
        }
        else
        {
            report.AppendLine("# FOUND NO CRASH ERRORS / SUSPECTS THAT MATCH THE CURRENT DATABASE #");
            report.AppendLine("Check below for mods that can cause frequent crashes and other problems.");
            report.AppendLine();
        }
    }

    private void GenerateSuspectEntry(Suspect suspect, StringBuilder report)
    {
        report.AppendLine(_template.FormatSuspect(
            suspect.Name,
            suspect.Description,
            suspect.SeverityScore,
            suspect.Evidence,
            suspect.Recommendation));

        if (suspect.RelatedFiles.Any())
            report.AppendLine($"  **Related Files:** {string.Join(", ", suspect.RelatedFiles)}");
    }

    private void GenerateSettingsSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        if (!result.SettingsValidation.Issues.Any())
            return;

        report.AppendLine(_template.FormatSectionHeader("CHECKING IF NECESSARY FILES/SETTINGS ARE CORRECT..."));

        foreach (var issue in result.SettingsValidation.Issues.OrderByDescending(i => i.SeverityScore))
        {
            var severityIcon = _template.GetSeverityIcon(issue.SeverityScore);
            report.AppendLine($"\n[{severityIcon}] {issue.SettingName}");
            report.AppendLine($"  Current: {issue.CurrentValue}");
            report.AppendLine($"  Expected: {issue.ExpectedValue}");
            report.AppendLine($"  Description: {issue.Description}");
        }

        report.AppendLine();
    }

    private void GenerateModCheckSections(CrashLogAnalysisResult result, StringBuilder report)
    {
        if (!result.ModCompatibilityIssues.Any())
            return;

        var issueTypes = result.ModCompatibilityIssues
            .GroupBy(i => i.IssueType);

        foreach (var issueGroup in issueTypes)
        {
            report.AppendLine(_template.FormatSectionHeader($"CHECKING FOR MODS THAT {issueGroup.Key.ToUpper()}..."));

            foreach (var issue in issueGroup.OrderBy(i => i.ModName))
            {
                report.AppendLine($"\n• {issue.ModName}");
                report.AppendLine($"  Issue: {issue.Description}");
                if (issue.ConflictingMods.Any())
                    report.AppendLine($"  Conflicts with: {string.Join(", ", issue.ConflictingMods)}");
                if (!string.IsNullOrEmpty(issue.Resolution)) report.AppendLine($"  Resolution: {issue.Resolution}");
            }

            report.AppendLine();
        }
    }

    private void GeneratePluginSuspectSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        report.AppendLine(_template.FormatSectionHeader("SCANNING THE LOG FOR SPECIFIC (POSSIBLE) SUSPECTS..."));

        // Plugin limit warnings
        GeneratePluginLimitWarnings(result.PluginLimitStatus, report);

        report.AppendLine("# LIST OF (POSSIBLE) PLUGIN SUSPECTS #");

        if (result.ProblematicPlugins.Any())
        {
            foreach (var plugin in result.ProblematicPlugins)
                report.AppendLine(_template.FormatPlugin(
                    plugin.LoadOrder,
                    plugin.Name,
                    plugin.Status != PluginStatus.Active ? plugin.Status.ToString() : null,
                    !string.IsNullOrEmpty(plugin.Flags) ? plugin.Flags : null));
        }
        else if (!result.PluginLimitStatus.PluginsLoaded)
        {
            report.AppendLine("* [!] NOTICE : BUFFOUT 4 WAS NOT ABLE TO LOAD THE PLUGIN LIST FOR THIS CRASH LOG! *");
            report.AppendLine("  CLASSIC cannot perform the full scan. Provide or scan a different crash log");
            report.AppendLine("  OR copy-paste your *loadorder.txt* into your main CLASSIC folder.");
        }
        else
        {
            report.AppendLine("  No problematic plugins detected.");
        }

        report.AppendLine();
    }

    private void GeneratePluginLimitWarnings(PluginLimitStatus status, StringBuilder report)
    {
        if (status.ReachedLimit && !status.LimitCheckDisabled && status.PluginsLoaded)
        {
            report.AppendLine(
                "# 💀 CRITICAL : THE '[FF]' PLUGIN PREFIX MEANS YOU REACHED THE PLUGIN LIMIT OF 254 PLUGINS #");
            report.AppendLine();
        }
        else if (status.ReachedLimit && status.LimitCheckDisabled && status.PluginsLoaded)
        {
            report.AppendLine(
                "# ⚠️ WARNING : THE '[FF]' PLUGIN PREFIX WAS DETECTED BUT PLUGIN LIMIT CHECK IS DISABLED. #");
            report.AppendLine("This could indicates that your version of Buffout 4 NG is out of date.");
            report.AppendLine("Recommendation: Consider updating Buffout 4 NG to the latest version.");
            report.AppendLine("-----");
            report.AppendLine();
        }
    }

    private void GenerateFormIdSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        if (!result.SuspectFormIds.Any())
            return;

        report.AppendLine("# LIST OF (POSSIBLE) FORM ID SUSPECTS #");

        foreach (var formId in result.SuspectFormIds.OrderBy(f => f.PluginName))
            report.AppendLine(_template.FormatFormId(
                formId.FormIdValue,
                formId.PluginIndex,
                formId.LocalFormId,
                formId.PluginName,
                formId.FormType));
        report.AppendLine();
    }

    private void GenerateNamedRecordSection(CrashLogAnalysisResult result, StringBuilder report)
    {
        if (!result.NamedRecords.Any())
            return;

        report.AppendLine("# LIST OF DETECTED (NAMED) RECORDS #");

        foreach (var record in result.NamedRecords.OrderBy(r => r.Name))
        {
            report.AppendLine($"  {record.Name}");
            report.AppendLine($"    Type: {record.Type}");
            report.AppendLine($"    FormID: {record.FormId.FormIdValue:X8}");
            report.AppendLine($"    Plugin: {record.OriginPlugin.Name}");
        }

        report.AppendLine();
    }

    private void GenerateFooter(CrashLogAnalysisResult result, StringBuilder report)
    {
        report.AppendLine(_template.FormatFooter(result.ClassicVersion, result.GeneratedDate));
    }
}
