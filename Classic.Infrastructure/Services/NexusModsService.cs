using HtmlAgilityPack;
using Classic.Core.Exceptions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for checking versions from Nexus Mods
/// </summary>
public class NexusModsService : INexusModsService
{
    private static readonly ILogger Logger = Log.ForContext<NexusModsService>();
    private readonly HttpClient _httpClient;
    private readonly IVersionService _versionService;

    public NexusModsService(HttpClient httpClient, IVersionService versionService)
    {
        _httpClient = httpClient;
        _versionService = versionService;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CLASSIC-8-UpdateChecker");
    }

    public async Task<VersionInfo?> GetLatestVersionAsync(string gameId, string modId,
        CancellationToken cancellationToken = default)
    {
        // Constants based on Python implementation
        var nexusModUrl = $"https://www.nexusmods.com/{gameId}/mods/{modId}";
        const string versionPropertyName = "twitter:label1";
        const string versionPropertyValue = "Version";
        const string versionDataProperty = "twitter:data1";

        try
        {
            Logger.Debug("Fetching Nexus mod version from {Url}", nexusModUrl);

            var response = await _httpClient.GetAsync(nexusModUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning("Failed to fetch Nexus mod page: HTTP {StatusCode}", response.StatusCode);
                return null;
            }

            var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            // Find the meta tag that indicates version label
            var versionLabelTag = htmlDocument.DocumentNode
                .SelectSingleNode($"//meta[@property='{versionPropertyName}' and @content='{versionPropertyValue}']");

            if (versionLabelTag == null)
            {
                Logger.Debug("Version label meta tag not found on Nexus page");
                return null;
            }

            // Look for the meta tag with version data
            var versionDataTag = htmlDocument.DocumentNode
                .SelectSingleNode($"//meta[@property='{versionDataProperty}']");

            if (versionDataTag?.GetAttributeValue("content", string.Empty) is not { } versionString ||
                string.IsNullOrEmpty(versionString))
            {
                Logger.Debug("Version data meta tag not found or content is missing");
                return null;
            }

            var parsedVersion = _versionService.ParseVersion(versionString);

            if (parsedVersion != null)
                Logger.Debug("Successfully parsed Nexus version: {Version}", parsedVersion);
            else
                Logger.Debug("Failed to parse version string from Nexus: '{VersionString}'", versionString);

            return parsedVersion;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "Network error while fetching Nexus version from {Url}", nexusModUrl);
            throw new UpdateCheckException($"Failed to fetch version from Nexus Mods: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error parsing Nexus version from {Url}", nexusModUrl);
            throw new UpdateCheckException($"Failed to parse Nexus Mods version: {ex.Message}", ex);
        }
    }
}
