using Classic.Core.Interfaces;
using Classic.Core.Models;

namespace Classic.ScanLog.Analyzers;

/// <inheritdoc />
public class SettingsScanner : ISettingsScanner
{
    /// <inheritdoc />
    public IEnumerable<Suspect> Scan(CrashLog crashLog)
    {
        // TODO: Implement settings scanning logic.
        return Enumerable.Empty<Suspect>();
    }
}
