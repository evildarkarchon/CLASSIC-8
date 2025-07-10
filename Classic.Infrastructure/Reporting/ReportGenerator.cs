using Classic.Core.Interfaces;
using Classic.Core.Models;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Classic.Infrastructure.Reporting
{
    public class ReportGenerator : IReportGenerator
    {
        private readonly IFileSystem _fileSystem;

        public ReportGenerator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task GenerateReportAsync(CrashLog crashLog, ScanStatistics statistics, string outputPath,
            CancellationToken cancellationToken = default)
        {
            var reportBuilder = new StringBuilder();

            // Basic report formatting
            reportBuilder.AppendLine("--- Crash Log Analysis Report ---");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine($"Log File: {crashLog.FilePath}");
            reportBuilder.AppendLine($"Timestamp: {crashLog.Timestamp}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("--- Scan Statistics ---");
            reportBuilder.AppendLine($"Scan Duration: {statistics.Duration.TotalSeconds:F2} seconds");
            reportBuilder.AppendLine($"Files Scanned: {statistics.FilesScanned}");
            reportBuilder.AppendLine($"Plugins Found: {statistics.PluginsFound}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("--- Detected Suspects ---");

            if (crashLog.Suspects.Count > 0)
            {
                foreach (var suspect in crashLog.Suspects)
                {
                    reportBuilder.AppendLine(
                        $"- {suspect.Name}: {suspect.Description} (Confidence: {suspect.Confidence})");
                }
            }
            else
            {
                reportBuilder.AppendLine("No suspects identified.");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("--- End of Report ---");

            await _fileSystem.File.WriteAllTextAsync(outputPath, reportBuilder.ToString(), cancellationToken);
        }
    }
}
