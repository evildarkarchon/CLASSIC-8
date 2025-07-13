using System.Text.Json;
using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for interacting with GitHub API to fetch release information
/// </summary>
public class GitHubApiService : IGitHubApiService
{
    private static readonly ILogger Logger = Log.ForContext<GitHubApiService>();
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitHubApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CLASSIC-8-UpdateChecker");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<GitHubRelease?> GetLatestStableReleaseAsync(string owner, string repo,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        try
        {
            Logger.Debug("Fetching latest stable release from {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.Information("No latest release found for {Owner}/{Repo} (status 404)", owner, repo);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var release = JsonSerializer.Deserialize<GitHubRelease>(jsonContent, _jsonOptions);

            if (release?.IsPreRelease == true)
            {
                Logger.Warning("Latest release endpoint returned a prerelease for {Owner}/{Repo}", owner, repo);
                return null;
            }

            Logger.Debug("Successfully fetched latest stable release: {ReleaseName}", release?.Name);
            return release;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "HTTP error fetching latest stable release from {Url}", url);
            throw new UpdateCheckException($"Failed to fetch release information from GitHub: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON parsing error for GitHub release data from {Url}", url);
            throw new UpdateCheckException($"Failed to parse GitHub release data: {ex.Message}", ex);
        }
    }

    public async Task<GitHubRelease?> GetLatestPreReleaseAsync(string owner, string repo,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases";

        try
        {
            Logger.Debug("Fetching latest prerelease from {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(jsonContent, _jsonOptions);

            if (releases == null)
            {
                Logger.Warning("No releases found in response from {Url}", url);
                return null;
            }

            // Find the first prerelease (releases are ordered newest first)
            var preRelease = releases.FirstOrDefault(r => r.IsPreRelease);

            if (preRelease != null)
                Logger.Debug("Successfully fetched latest prerelease: {ReleaseName}", preRelease.Name);
            else
                Logger.Debug("No prereleases found for {Owner}/{Repo}", owner, repo);

            return preRelease;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "HTTP error fetching prereleases from {Url}", url);
            throw new UpdateCheckException($"Failed to fetch prerelease information from GitHub: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON parsing error for GitHub releases data from {Url}", url);
            throw new UpdateCheckException($"Failed to parse GitHub releases data: {ex.Message}", ex);
        }
    }

    public async Task<GitHubReleaseDetails?> GetReleaseDetailsAsync(string owner, string repo,
        CancellationToken cancellationToken = default)
    {
        var latestUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        var allReleasesUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";

        GitHubRelease? latestEndpointRelease = null;
        GitHubRelease? topOfListRelease = null;

        try
        {
            Logger.Debug("Fetching release details for {Owner}/{Repo}", owner, repo);

            // Fetch from latest endpoint
            try
            {
                var latestResponse = await _httpClient.GetAsync(latestUrl, cancellationToken);
                if (latestResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    latestResponse.EnsureSuccessStatusCode();
                    var latestJsonContent = await latestResponse.Content.ReadAsStringAsync(cancellationToken);
                    latestEndpointRelease = JsonSerializer.Deserialize<GitHubRelease>(latestJsonContent, _jsonOptions);
                }
                else
                {
                    Logger.Information("No latest release found for {Owner}/{Repo} (status 404)", owner, repo);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to fetch from latest endpoint for {Owner}/{Repo}", owner, repo);
            }

            // Fetch from all releases list
            var allReleasesResponse = await _httpClient.GetAsync(allReleasesUrl, cancellationToken);
            allReleasesResponse.EnsureSuccessStatusCode();

            var allReleasesJsonContent = await allReleasesResponse.Content.ReadAsStringAsync(cancellationToken);
            var allReleases = JsonSerializer.Deserialize<GitHubRelease[]>(allReleasesJsonContent, _jsonOptions);

            if (allReleases?.Length > 0) topOfListRelease = allReleases[0];

            var areSameReleaseById = latestEndpointRelease?.Id == topOfListRelease?.Id &&
                                     latestEndpointRelease?.Id != 0;

            var details = new GitHubReleaseDetails
            {
                LatestEndpointRelease = latestEndpointRelease,
                TopOfListRelease = topOfListRelease,
                AreSameReleaseById = areSameReleaseById
            };

            Logger.Debug(
                "Release details comparison: LatestEndpoint={LatestName}, TopOfList={TopName}, SameById={SameById}",
                latestEndpointRelease?.Name, topOfListRelease?.Name, areSameReleaseById);

            return details;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "HTTP error fetching release details for {Owner}/{Repo}", owner, repo);
            throw new UpdateCheckException($"Failed to fetch release details from GitHub: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON parsing error for GitHub release details for {Owner}/{Repo}", owner, repo);
            throw new UpdateCheckException($"Failed to parse GitHub release details: {ex.Message}", ex);
        }
    }
}
