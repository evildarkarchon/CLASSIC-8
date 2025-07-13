using System.Reflection;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for parsing and comparing versions
/// </summary>
public class VersionService : IVersionService
{
    private static readonly ILogger Logger = Log.ForContext<VersionService>();

    public VersionInfo? ParseVersion(string? versionString)
    {
        if (VersionInfo.TryParse(versionString, out var version))
        {
            Logger.Debug("Successfully parsed version: {VersionString} -> {Version}", versionString, version);
            return version;
        }

        Logger.Debug("Failed to parse version string: {VersionString}", versionString);
        return null;
    }

    public int CompareVersions(VersionInfo? version1, VersionInfo? version2)
    {
        if (version1 is null && version2 is null) return 0;
        if (version1 is null) return -1;
        if (version2 is null) return 1;

        var result = version1.CompareTo(version2);
        Logger.Debug("Version comparison: {Version1} vs {Version2} = {Result}", version1, version2, result);
        return result;
    }

    public bool IsUpdateAvailable(VersionInfo? currentVersion, VersionInfo? latestVersion)
    {
        if (currentVersion is null || latestVersion is null)
        {
            Logger.Debug("Cannot determine if update is available - one or both versions are null");
            return false;
        }

        var isUpdateAvailable = currentVersion < latestVersion;
        Logger.Debug(
            "Update check: Current={CurrentVersion}, Latest={LatestVersion}, UpdateAvailable={UpdateAvailable}",
            currentVersion, latestVersion, isUpdateAvailable);

        return isUpdateAvailable;
    }

    /// <summary>
    /// Gets the current application version from the executing assembly
    /// </summary>
    /// <returns>Current application version</returns>
    public VersionInfo? GetCurrentApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version;

            if (assemblyVersion != null)
            {
                var version = new VersionInfo(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
                Logger.Debug("Current application version: {Version}", version);
                return version;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get current application version");
        }

        Logger.Warning("Could not determine current application version");
        return null;
    }
}
