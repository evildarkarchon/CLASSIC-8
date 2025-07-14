using HtmlAgilityPack;
using Classic.Core.Interfaces;
using Classic.Core.Models;

namespace Classic.Infrastructure.Services.UpdateSources;

/// <summary>
/// Nexus Mods update source implementation
/// </summary>
public class NexusModsUpdateSource : UpdateSourceBase
{
    private readonly string _gameId;
    private readonly string _modId;

    // Constants based on Python implementation
    private const string VersionPropertyName = "twitter:label1";
    private const string VersionPropertyValue = "Version";
    private const string VersionDataProperty = "twitter:data1";

    public NexusModsUpdateSource(HttpClient httpClient, IVersionService versionService, 
        string gameId = "fallout4", string modId = "56255") 
        : base(httpClient, versionService)
    {
        _gameId = gameId;
        _modId = modId;
        ConfigureHttpClient();
    }

    public override string SourceName => "Nexus";
    public override bool SupportsPreReleases => false; // Nexus doesn't support pre-releases

    protected override async Task<UpdateSourceResult> GetLatestVersionInternalAsync(bool includePreReleases, 
        CancellationToken cancellationToken)
    {
        var nexusModUrl = $"https://www.nexusmods.com/{_gameId}/mods/{_modId}";

        Logger.Debug("Fetching Nexus mod version from {Url}", nexusModUrl);

        var response = await HttpClient.GetAsync(nexusModUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = $"Failed to fetch Nexus mod page: HTTP {response.StatusCode}";
            Logger.Warning(errorMessage);
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlContent);

        // Find the meta tag that indicates version label
        var versionLabelTag = htmlDocument.DocumentNode
            .SelectSingleNode($"//meta[@property='{VersionPropertyName}' and @content='{VersionPropertyValue}']");

        if (versionLabelTag == null)
        {
            var errorMessage = "Version label meta tag not found on Nexus page";
            Logger.Debug(errorMessage);
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }

        // Look for the meta tag with version data
        var versionDataTag = htmlDocument.DocumentNode
            .SelectSingleNode($"//meta[@property='{VersionDataProperty}']");

        if (versionDataTag?.GetAttributeValue("content", string.Empty) is not { } versionString ||
            string.IsNullOrEmpty(versionString))
        {
            var errorMessage = "Version data meta tag not found or content is missing";
            Logger.Debug(errorMessage);
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }

        var parsedVersion = ParseVersion(versionString);

        if (parsedVersion == null)
        {
            var errorMessage = $"Failed to parse version string from Nexus: '{versionString}'";
            return UpdateSourceResult.Failure(errorMessage, SourceName);
        }

        Logger.Debug("Successfully parsed Nexus version: {Version}", parsedVersion);
        return UpdateSourceResult.Success(parsedVersion, SourceName);
    }
}
