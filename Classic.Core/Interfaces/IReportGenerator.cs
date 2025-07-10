using Classic.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Classic.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a report generator.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// Asynchronously generates a report from a crash log and scan statistics.
        /// </summary>
        /// <param name="crashLog">The crash log data.</param>
        /// <param name="statistics">The statistics from the scan.</param>
        /// <param name="outputPath">The path to save the generated report.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task GenerateReportAsync(CrashLog crashLog, ScanStatistics statistics, string outputPath,
            CancellationToken cancellationToken = default);
    }
}
