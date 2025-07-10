namespace Classic.Core.Interfaces;

/// <summary>
///     Detects installed mods.
/// </summary>
public interface IModDetector
{
    /// <summary>
    ///     Detects installed mods.
    /// </summary>
    /// <returns>An enumerable of installed mods.</returns>
    IEnumerable<string> Detect();
}
