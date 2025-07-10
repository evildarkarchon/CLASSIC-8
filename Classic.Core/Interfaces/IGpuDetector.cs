namespace Classic.Core.Interfaces;

/// <summary>
///     Detects the GPU.
/// </summary>
public interface IGpuDetector
{
    /// <summary>
    ///     Detects the GPU.
    /// </summary>
    /// <returns>The GPU name.</returns>
    string? Detect();
}
