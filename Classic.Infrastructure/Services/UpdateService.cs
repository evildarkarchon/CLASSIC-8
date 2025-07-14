using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Main service for checking application updates from various sources using strategy pattern
/// </summary>
public class UpdateService : IUpdateService
{
    private static readonly ILogger Logger = Log.ForContext<UpdateService>();
    private readonly IUpdateSourceFactory _updateSourceFactory;
    private readonly IVersionService _versionService;
    private readonly ISettingsService _settingsService;

    public UpdateService(
        IUpdateSourceFactory updateSourceFactory,
        IVersionService versionService,
        ISettingsService settingsService)
    {
        _updateSourceFactory = updateSourceFactory;
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
            var errorMessage = $"Invalid update source: {updateSource}. Valid sources are: {GetValidSourcesString()}";
            Logger.Warning(errorMessage);
            return UpdateCheckResult.Failure(errorMessage, updateSource);
        }

        try
        {
            var sourceNames = GetSourceNamesFromUpdateSource(updateSource);
            var sourceResults = new List<UpdateSourceResult>();

            // Check each specified source
            foreach (var sourceName in sourceNames)
            {
                var source = _updateSourceFactory.GetSource(sourceName);
                if (source == null)
                {
                    Logger.Warning("Update source '{SourceName}' not found", sourceName);
                    continue;
                }

                // Skip sources that don't support pre-releases if pre-releases are requested
                var effectiveIncludePreReleases = includePreReleases && source.SupportsPreReleases;

                var result = await source.GetLatestVersionAsync(effectiveIncludePreReleases, cancellationToken);
                sourceResults.Add(result);
            }

            // Check if all sources failed
            var successfulResults = sourceResults.Where(r => r.IsSuccess).ToList();
            if (successfulResults.Count == 0)
            {
                var failedSources = sourceResults.Select(r => $"{r.SourceName}: {r.ErrorMessage}");
                var errorMessage = $"All update sources failed: {string.Join("; ", failedSources)}";
                Logger.Error(errorMessage);
                return UpdateCheckResult.Failure(errorMessage, updateSource);
            }

            // Determine the latest version from successful results
            var latestResult = GetLatestVersionFromResults(successfulResults);
            var isUpdateAvailable = _versionService.IsUpdateAvailable(currentVersion, latestResult.Version);

            Logger.Information("Update check completed - Current: {Current}, Latest: {Latest}, Available: {Available}",
                currentVersion, latestResult.Version, isUpdateAvailable);

            return UpdateCheckResult.Success(currentVersion, latestResult.Version, latestResult.Release,
                updateSource, isUpdateAvailable);
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
        // Try to get version from VersionService (assembly version)
        if (_versionService is VersionService versionService)
            return versionService.GetCurrentApplicationVersion();

        Logger.Warning("Could not get current version from VersionService");
        return null;
    }

    private IEnumerable<string> GetSourceNamesFromUpdateSource(string updateSource)
    {
        return updateSource.ToLowerInvariant() switch
        {
            "both" => new[] { "GitHub", "Nexus" },
            "github" => new[] { "GitHub" },
            "nexus" => new[] { "Nexus" },
            _ => new[] { updateSource } // Allow custom source names
        };
    }

    private UpdateSourceResult GetLatestVersionFromResults(IEnumerable<UpdateSourceResult> results)
    {
        var resultsList = results.ToList();
        if (resultsList.Count == 0)
            throw new InvalidOperationException("No results provided");

        if (resultsList.Count == 1)
            return resultsList[0];

        // Find the result with the highest version
        var latestResult = resultsList
            .Where(r => r.Version != null)
            .OrderByDescending(r => r.Version)
            .FirstOrDefault();

        if (latestResult == null)
            throw new InvalidOperationException("No valid versions found in results");

        return latestResult;
    }

    private bool IsValidUpdateSource(string updateSource)
    {
        if (string.IsNullOrWhiteSpace(updateSource))
            return false;

        // Check standard source names
        if (updateSource.Equals("Both", StringComparison.OrdinalIgnoreCase) ||
            updateSource.Equals("GitHub", StringComparison.OrdinalIgnoreCase) ||
            updateSource.Equals("Nexus", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if it's a valid registered source
        return _updateSourceFactory.GetSource(updateSource) != null;
    }

    private string GetValidSourcesString()
    {
        var registeredSources = _updateSourceFactory.GetAllSources().Select(s => s.SourceName);
        var standardSources = new[] { "Both", "GitHub", "Nexus" };
        var allSources = standardSources.Concat(registeredSources).Distinct();
        return string.Join(", ", allSources);
    }
}
