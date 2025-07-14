using System.Text.Json;
using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;

namespace Classic.Infrastructure.Services.UpdateSources;

/// <summary>
/// GitHub update source implementation
/// </summary>
public class GitHubUpdateSource : UpdateSourceBase
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubUpdateSource(HttpClient httpClient, IVersionService versionService, 
        string owner = "evildarkarchon", string repo = "CLASSIC-Fallout4") 
        : base(httpClient, versionService)
    {
        _owner = owner;
        _repo = repo;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        ConfigureHttpClient();
    }

    public override string SourceName => "GitHub";
    public override bool SupportsPreReleases => true;

    protected override async Task<UpdateSourceResult> GetLatestVersionInternalAsync(bool includePreReleases, 
        CancellationToken cancellationToken)
    {
        var releaseDetails = await GetReleaseDetailsAsync(cancellationToken);

        if (releaseDetails == null)
        {
            return UpdateSourceResult.Failure("No release details found from GitHub", SourceName);
        }

        var candidateVersions = new List<(VersionInfo version, GitHubRelease release)>();

        // Check latest endpoint release
        if (releaseDetails.LatestEndpointRelease?.Version != null &&
            (!releaseDetails.LatestEndpointRelease.IsPreRelease || includePreReleases))
        {
            candidateVersions.Add((releaseDetails.LatestEndpointRelease.Version, releaseDetails.LatestEndpointRelease));
        }

        // Check top of list release
        if (releaseDetails.TopOfListRelease?.Version != null &&
            (!releaseDetails.TopOfListRelease.IsPreRelease || includePreReleases))
        {
            candidateVersions.Add((releaseDetails.TopOfListRelease.Version, releaseDetails.TopOfListRelease));
        }

        if (candidateVersions.Count == 0)
        {
            var reason = includePreReleases ? "No releases found" : "No stable releases found";
            return UpdateSourceResult.Failure(reason, SourceName);
        }

        // Return the highest version
        var latest = candidateVersions.OrderByDescending(c => c.version).First();
        Logger.Information("Determined latest GitHub version: {Version}", latest.version);

        return UpdateSourceResult.Success(latest.version, SourceName, latest.release);
    }

    private async Task<GitHubReleaseDetails?> GetReleaseDetailsAsync(CancellationToken cancellationToken)
    {
        var latestUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
        var allReleasesUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases";

        GitHubRelease? latestEndpointRelease = null;
        GitHubRelease? topOfListRelease = null;

        Logger.Debug("Fetching release details for {Owner}/{Repo}", _owner, _repo);

        // Fetch from latest endpoint
        try
        {
            var latestResponse = await HttpClient.GetAsync(latestUrl, cancellationToken);
            if (latestResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                latestResponse.EnsureSuccessStatusCode();
                var latestJsonContent = await latestResponse.Content.ReadAsStringAsync(cancellationToken);
                latestEndpointRelease = JsonSerializer.Deserialize<GitHubRelease>(latestJsonContent, _jsonOptions);
            }
            else
            {
                Logger.Information("No latest release found for {Owner}/{Repo} (status 404)", _owner, _repo);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to fetch from latest endpoint for {Owner}/{Repo}", _owner, _repo);
        }

        // Fetch from all releases list
        var allReleasesResponse = await HttpClient.GetAsync(allReleasesUrl, cancellationToken);
        allReleasesResponse.EnsureSuccessStatusCode();

        var allReleasesJsonContent = await allReleasesResponse.Content.ReadAsStringAsync(cancellationToken);
        var allReleases = JsonSerializer.Deserialize<GitHubRelease[]>(allReleasesJsonContent, _jsonOptions);

        if (allReleases?.Length > 0) 
            topOfListRelease = allReleases[0];

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
}
