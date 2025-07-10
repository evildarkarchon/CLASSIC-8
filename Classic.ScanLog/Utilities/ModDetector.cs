using Classic.Core.Interfaces;

namespace Classic.ScanLog.Utilities;

/// <inheritdoc />
public class ModDetector : IModDetector
{
    /// <inheritdoc />
    public IEnumerable<string> Detect()
    {
        // TODO: Implement mod detection logic.
        return Enumerable.Empty<string>();
    }
}
