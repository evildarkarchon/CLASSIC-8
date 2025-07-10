using Classic.Core.Interfaces;
using Classic.Core.Models;

namespace Classic.ScanLog.Analyzers;

/// <inheritdoc />
public class RecordScanner : IRecordScanner
{
    /// <inheritdoc />
    public IEnumerable<Suspect> Scan(CrashLog crashLog)
    {
        // TODO: Implement record scanning logic.
        return Enumerable.Empty<Suspect>();
    }
}
