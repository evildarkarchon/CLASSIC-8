using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
///     Scans for specific settings in the crash log.
/// </summary>
public interface ISettingsScanner
{
    /// <summary>
    ///     Scans the crash log for specific settings.
    /// </summary>
    /// <param name="crashLog">The crash log to scan.</param>
    /// <returns>An enumerable of suspects found.</returns>
    IEnumerable<Suspect> Scan(CrashLog crashLog);
}
