using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Represents a source for checking application updates
/// </summary>
public interface IUpdateSource
{
    /// <summary>
    /// The name of this update source (e.g., "GitHub", "Nexus")
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Whether this source supports pre-release versions
    /// </summary>
    bool SupportsPreReleases { get; }

    /// <summary>
    /// Gets the latest version from this update source
    /// </summary>
    /// <param name="includePreReleases">Whether to include pre-releases (ignored if not supported)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version information and optional release details</returns>
    Task<UpdateSourceResult> GetLatestVersionAsync(bool includePreReleases = false, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating update sources
/// </summary>
public interface IUpdateSourceFactory
{
    /// <summary>
    /// Gets an update source by name (case-insensitive)
    /// </summary>
    /// <param name="sourceName">Name of the update source</param>
    /// <returns>Update source instance, or null if not found</returns>
    IUpdateSource? GetSource(string sourceName);

    /// <summary>
    /// Gets all available update sources
    /// </summary>
    /// <returns>Collection of all registered update sources</returns>
    IEnumerable<IUpdateSource> GetAllSources();

    /// <summary>
    /// Gets update sources that support pre-releases
    /// </summary>
    /// <returns>Collection of update sources that support pre-releases</returns>
    IEnumerable<IUpdateSource> GetPreReleaseCapableSources();

    /// <summary>
    /// Registers an update source
    /// </summary>
    /// <param name="source">Update source to register</param>
    void RegisterSource(IUpdateSource source);
}
