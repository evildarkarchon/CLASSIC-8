using System.Text.Json.Serialization;

namespace Classic.Core.Models;

/// <summary>
/// Represents version information with comparison capabilities
/// </summary>
public record VersionInfo(int Major, int Minor, int Patch, string? PreRelease = null)
{
    public static VersionInfo Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            throw new ArgumentException("Version string cannot be null or empty", nameof(versionString));

        // Remove leading 'v' if present
        var cleanVersion = versionString.TrimStart('v', 'V');
        
        // Split by '-' to separate version from prerelease
        var parts = cleanVersion.Split('-', 2);
        var versionPart = parts[0];
        var preRelease = parts.Length > 1 ? parts[1] : null;
        
        // Parse version numbers
        var versionNumbers = versionPart.Split('.');
        if (versionNumbers.Length < 2)
            throw new ArgumentException($"Invalid version format: {versionString}", nameof(versionString));

        if (!int.TryParse(versionNumbers[0], out var major))
            throw new ArgumentException($"Invalid major version: {versionNumbers[0]}", nameof(versionString));
        
        if (!int.TryParse(versionNumbers[1], out var minor))
            throw new ArgumentException($"Invalid minor version: {versionNumbers[1]}", nameof(versionString));

        var patch = 0;
        if (versionNumbers.Length > 2 && !int.TryParse(versionNumbers[2], out patch))
            throw new ArgumentException($"Invalid patch version: {versionNumbers[2]}", nameof(versionString));

        return new VersionInfo(major, minor, patch, preRelease);
    }

    public static bool TryParse(string? versionString, out VersionInfo? version)
    {
        version = null;
        try
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return false;
                
            version = Parse(versionString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        return string.IsNullOrEmpty(PreRelease) ? version : $"{version}-{PreRelease}";
    }

    public static bool operator >(VersionInfo left, VersionInfo right) => left.CompareTo(right) > 0;
    public static bool operator <(VersionInfo left, VersionInfo right) => left.CompareTo(right) < 0;
    public static bool operator >=(VersionInfo left, VersionInfo right) => left.CompareTo(right) >= 0;
    public static bool operator <=(VersionInfo left, VersionInfo right) => left.CompareTo(right) <= 0;

    public int CompareTo(VersionInfo? other)
    {
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // Stable versions are greater than prereleases
        if (!IsPreRelease && other.IsPreRelease) return 1;
        if (IsPreRelease && !other.IsPreRelease) return -1;
        
        // Both are prereleases or both are stable
        return string.Compare(PreRelease, other.PreRelease, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Represents a GitHub release
/// </summary>
public record GitHubRelease
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("prerelease")]
    public bool IsPreRelease { get; init; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; init; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    public VersionInfo? Version => VersionInfo.TryParse(Name, out var version) ? version : null;
}

/// <summary>
/// Represents the result of an update check operation
/// </summary>
public record UpdateCheckResult
{
    public bool IsSuccess { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public VersionInfo? CurrentVersion { get; init; }
    public VersionInfo? LatestVersion { get; init; }
    public GitHubRelease? LatestRelease { get; init; }
    public string? ErrorMessage { get; init; }
    public string UpdateSource { get; init; } = string.Empty;
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    public static UpdateCheckResult Success(VersionInfo? currentVersion, VersionInfo? latestVersion, 
        GitHubRelease? latestRelease, string updateSource, bool isUpdateAvailable)
    {
        return new UpdateCheckResult
        {
            IsSuccess = true,
            IsUpdateAvailable = isUpdateAvailable,
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            LatestRelease = latestRelease,
            UpdateSource = updateSource
        };
    }

    public static UpdateCheckResult Failure(string errorMessage, string updateSource)
    {
        return new UpdateCheckResult
        {
            IsSuccess = false,
            IsUpdateAvailable = false,
            ErrorMessage = errorMessage,
            UpdateSource = updateSource
        };
    }
}

/// <summary>
/// Contains details about GitHub release comparisons
/// </summary>
public record GitHubReleaseDetails
{
    public GitHubRelease? LatestEndpointRelease { get; init; }
    public GitHubRelease? TopOfListRelease { get; init; }
    public bool AreSameReleaseById { get; init; }
}