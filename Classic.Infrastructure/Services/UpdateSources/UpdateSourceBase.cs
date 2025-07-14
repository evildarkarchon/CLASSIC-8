using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services.UpdateSources;

/// <summary>
/// Base class for update source implementations providing common functionality
/// </summary>
public abstract class UpdateSourceBase : IUpdateSource
{
    protected static readonly ILogger Logger = Log.ForContext<UpdateSourceBase>();
    protected readonly HttpClient HttpClient;
    protected readonly IVersionService VersionService;

    protected UpdateSourceBase(HttpClient httpClient, IVersionService versionService)
    {
        HttpClient = httpClient;
        VersionService = versionService;
    }

    public abstract string SourceName { get; }
    public abstract bool SupportsPreReleases { get; }

    public async Task<UpdateSourceResult> GetLatestVersionAsync(bool includePreReleases = false, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.Debug("Checking for updates from {SourceName}, PreReleases: {IncludePreReleases}", 
                SourceName, includePreReleases);

            if (includePreReleases && !SupportsPreReleases)
            {
                Logger.Debug("{SourceName} does not support pre-releases, checking stable only", SourceName);
                includePreReleases = false;
            }

            var result = await GetLatestVersionInternalAsync(includePreReleases, cancellationToken);

            if (result.IsSuccess)
            {
                Logger.Information("Successfully retrieved version {Version} from {SourceName}", 
                    result.Version, SourceName);
            }
            else
            {
                Logger.Warning("Failed to retrieve version from {SourceName}: {Error}", 
                    SourceName, result.ErrorMessage);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Network error accessing {SourceName}: {ex.Message}";
            Logger.Error(ex, "Network error for {SourceName}", SourceName);
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }
        catch (UpdateCheckException ex)
        {
            Logger.Error(ex, "Update check error for {SourceName}", SourceName);
            return UpdateSourceResult.Failure(ex.Message, SourceName);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error accessing {SourceName}: {ex.Message}";
            Logger.Error(ex, "Unexpected error for {SourceName}", SourceName);
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }
    }

    /// <summary>
    /// Template method for specific update source implementations
    /// </summary>
    /// <param name="includePreReleases">Whether to include pre-releases</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update source result</returns>
    protected abstract Task<UpdateSourceResult> GetLatestVersionInternalAsync(bool includePreReleases, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Helper method to parse version strings consistently
    /// </summary>
    /// <param name="versionString">Version string to parse</param>
    /// <returns>Parsed version or null if parsing failed</returns>
    protected VersionInfo? ParseVersion(string? versionString)
    {
        var version = VersionService.ParseVersion(versionString);
        if (version == null)
        {
            Logger.Debug("Failed to parse version string from {SourceName}: '{VersionString}'", 
                SourceName, versionString);
        }
        return version;
    }

    /// <summary>
    /// Helper method to configure HTTP client headers consistently
    /// </summary>
    /// <param name="additionalHeaders">Additional headers to add</param>
    protected void ConfigureHttpClient(IDictionary<string, string>? additionalHeaders = null)
    {
        if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "CLASSIC-8-UpdateChecker");
        }

        if (additionalHeaders != null)
        {
            foreach (var (key, value) in additionalHeaders)
            {
                if (!HttpClient.DefaultRequestHeaders.Contains(key))
                {
                    HttpClient.DefaultRequestHeaders.Add(key, value);
                }
            }
        }
    }
}
