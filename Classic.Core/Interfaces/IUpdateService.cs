using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for checking application updates from various sources
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks for updates asynchronously based on settings configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the update check</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for updates from a specific source
    /// </summary>
    /// <param name="updateSource">Source to check (GitHub, Nexus, Both)</param>
    /// <param name="includePreReleases">Whether to include pre-releases</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the update check</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(string updateSource, bool includePreReleases,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current application version
    /// </summary>
    /// <returns>Current version information</returns>
    VersionInfo? GetCurrentVersion();
}

/// <summary>
/// Service for interacting with GitHub API for release information
/// </summary>
public interface IGitHubApiService
{
    /// <summary>
    /// Gets the latest stable release from GitHub
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest stable release or null if not found</returns>
    Task<GitHubRelease?> GetLatestStableReleaseAsync(string owner, string repo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest pre-release from GitHub
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest pre-release or null if not found</returns>
    Task<GitHubRelease?> GetLatestPreReleaseAsync(string owner, string repo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed release information comparing latest endpoint and top of list
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed release comparison information</returns>
    Task<GitHubReleaseDetails?> GetReleaseDetailsAsync(string owner, string repo,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for checking versions from Nexus Mods
/// </summary>
public interface INexusModsService
{
    /// <summary>
    /// Gets the latest version from Nexus Mods
    /// </summary>
    /// <param name="gameId">Game identifier (e.g., fallout4)</param>
    /// <param name="modId">Mod identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest version from Nexus or null if not found</returns>
    Task<VersionInfo?> GetLatestVersionAsync(string gameId, string modId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for parsing and comparing versions
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Parses a version string into a VersionInfo object
    /// </summary>
    /// <param name="versionString">Version string to parse</param>
    /// <returns>Parsed version information</returns>
    VersionInfo? ParseVersion(string? versionString);

    /// <summary>
    /// Compares two versions
    /// </summary>
    /// <param name="version1">First version</param>
    /// <param name="version2">Second version</param>
    /// <returns>Comparison result (-1, 0, 1)</returns>
    int CompareVersions(VersionInfo? version1, VersionInfo? version2);

    /// <summary>
    /// Determines if an update is available
    /// </summary>
    /// <param name="currentVersion">Current version</param>
    /// <param name="latestVersion">Latest available version</param>
    /// <returns>True if update is available</returns>
    bool IsUpdateAvailable(VersionInfo? currentVersion, VersionInfo? latestVersion);
}
