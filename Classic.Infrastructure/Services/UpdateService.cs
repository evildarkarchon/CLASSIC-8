using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Main service for checking application updates from various sources
/// </summary>
public class UpdateService : IUpdateService
{
    private static readonly ILogger Logger = Log.ForContext<UpdateService>();
    private readonly IGitHubApiService _gitHubApiService;
    private readonly INexusModsService _nexusModsService;
    private readonly IVersionService _versionService;
    private readonly ISettingsService _settingsService;

    // Constants based on Python implementation
    private const string RepoOwner = "evildarkarchon";
    private const string RepoName = "CLASSIC-Fallout4";
    private const string NexusGameId = "fallout4";
    private const string NexusModId = "56255";

    public UpdateService(
        IGitHubApiService gitHubApiService,
        INexusModsService nexusModsService,
        IVersionService versionService,
        ISettingsService settingsService)
    {
        _gitHubApiService = gitHubApiService;
        _nexusModsService = nexusModsService;
        _versionService = versionService;
        _settingsService = settingsService;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        Logger.Debug("Starting update check with settings configuration");

        var settings = _settingsService.Settings;
        var updateSource = settings.UpdateSource ?? "Both";
        var includePreReleases = settings.UsePreReleases;
        var updateCheckEnabled = settings.UpdateCheck;

        if (!updateCheckEnabled)
        {
            Logger.Information("Update check is disabled in settings");
            return UpdateCheckResult.Failure("Update check is disabled in settings", updateSource);
        }

        return await CheckForUpdatesAsync(updateSource, includePreReleases, cancellationToken);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string updateSource, bool includePreReleases,
        CancellationToken cancellationToken = default)
    {
        Logger.Debug("Starting update check - Source: {UpdateSource}, PreReleases: {IncludePreReleases}",
            updateSource, includePreReleases);

        var currentVersion = GetCurrentVersion();

        if (!IsValidUpdateSource(updateSource))
        {
            var errorMessage = $"Invalid update source: {updateSource}. Valid sources are: GitHub, Nexus, Both";
            Logger.Warning(errorMessage);
            return UpdateCheckResult.Failure(errorMessage, updateSource);
        }

        var useGitHub = updateSource is "Both" or "GitHub";
        var useNexus = updateSource is "Both" or "Nexus" && !includePreReleases; // Nexus doesn't support prereleases

        VersionInfo? gitHubVersion = null;
        VersionInfo? nexusVersion = null;
        GitHubRelease? latestRelease = null;

        var gitHubFailed = false;
        var nexusFailed = false;

        try
        {
            // Check GitHub
            if (useGitHub)
                try
                {
                    var (version, release) = await GetGitHubVersionAsync(includePreReleases, cancellationToken);
                    gitHubVersion = version;
                    latestRelease = release;
                    Logger.Information("GitHub version check completed: {Version}", gitHubVersion);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "GitHub version check failed");
                    gitHubFailed = true;
                }

            // Check Nexus (only for stable releases)
            if (useNexus)
                try
                {
                    nexusVersion =
                        await _nexusModsService.GetLatestVersionAsync(NexusGameId, NexusModId, cancellationToken);
                    Logger.Information("Nexus version check completed: {Version}", nexusVersion);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Nexus version check failed");
                    nexusFailed = true;
                }

            // Check for source failures
            CheckSourceFailuresAndThrow(useGitHub, useNexus, gitHubFailed, nexusFailed);

            // Determine the latest version
            var latestVersion = GetLatestVersion(gitHubVersion, nexusVersion);
            var isUpdateAvailable = _versionService.IsUpdateAvailable(currentVersion, latestVersion);

            Logger.Information("Update check completed - Current: {Current}, Latest: {Latest}, Available: {Available}",
                currentVersion, latestVersion, isUpdateAvailable);

            return UpdateCheckResult.Success(currentVersion, latestVersion, latestRelease, updateSource,
                isUpdateAvailable);
        }
        catch (UpdateCheckException)
        {
            throw; // Re-throw update check exceptions as-is
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error during update check: {ex.Message}";
            Logger.Error(ex, "Unexpected error during update check");
            throw new UpdateCheckException(errorMessage, ex);
        }
    }

    public VersionInfo? GetCurrentVersion()
    {
        // Try to get version from VersionService first (assembly version)
        if (_versionService is VersionService versionService) return versionService.GetCurrentApplicationVersion();

        Logger.Warning("Could not get current version from VersionService");
        return null;
    }

    private async Task<(VersionInfo? version, GitHubRelease? release)> GetGitHubVersionAsync(bool includePreReleases,
        CancellationToken cancellationToken)
    {
        Logger.Debug("Fetching GitHub release details for {Owner}/{Repo}", RepoOwner, RepoName);

        var releaseDetails = await _gitHubApiService.GetReleaseDetailsAsync(RepoOwner, RepoName, cancellationToken);

        if (releaseDetails == null)
        {
            Logger.Warning("No release details found from GitHub");
            return (null, null);
        }

        var candidateVersions = new List<(VersionInfo version, GitHubRelease release)>();

        // Check latest endpoint release
        if (releaseDetails.LatestEndpointRelease?.Version != null &&
            (!releaseDetails.LatestEndpointRelease.IsPreRelease || includePreReleases))
            candidateVersions.Add((releaseDetails.LatestEndpointRelease.Version, releaseDetails.LatestEndpointRelease));

        // Check top of list release
        if (releaseDetails.TopOfListRelease?.Version != null &&
            (!releaseDetails.TopOfListRelease.IsPreRelease || includePreReleases))
            candidateVersions.Add((releaseDetails.TopOfListRelease.Version, releaseDetails.TopOfListRelease));

        if (candidateVersions.Count == 0)
        {
            Logger.Warning("No suitable releases found on GitHub");
            return (null, null);
        }

        // Return the highest version
        var latest = candidateVersions.OrderByDescending(c => c.version).First();
        Logger.Information("Determined latest GitHub version: {Version}", latest.version);

        return latest;
    }

    private static VersionInfo? GetLatestVersion(VersionInfo? gitHubVersion, VersionInfo? nexusVersion)
    {
        if (gitHubVersion == null) return nexusVersion;
        if (nexusVersion == null) return gitHubVersion;

        return gitHubVersion > nexusVersion ? gitHubVersion : nexusVersion;
    }

    private static bool IsValidUpdateSource(string updateSource)
    {
        return updateSource is "Both" or "GitHub" or "Nexus";
    }

    private static void CheckSourceFailuresAndThrow(bool useGitHub, bool useNexus, bool gitHubFailed, bool nexusFailed)
    {
        if (useGitHub && !useNexus && gitHubFailed)
            throw new UpdateCheckException(
                "Unable to fetch version information from GitHub (selected as only source).");

        if (useNexus && !useGitHub && nexusFailed)
            throw new UpdateCheckException("Unable to fetch version information from Nexus (selected as only source).");

        if (useGitHub && useNexus && gitHubFailed && nexusFailed)
            throw new UpdateCheckException("Unable to fetch version information from both GitHub and Nexus.");
    }
}
