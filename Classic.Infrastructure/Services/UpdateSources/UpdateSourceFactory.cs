using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.Services.UpdateSources;

/// <summary>
/// Factory for managing update sources
/// </summary>
public class UpdateSourceFactory : IUpdateSourceFactory
{
    private static readonly ILogger Logger = Log.ForContext<UpdateSourceFactory>();
    private readonly Dictionary<string, IUpdateSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    public UpdateSourceFactory(IEnumerable<IUpdateSource> sources)
    {
        foreach (var source in sources)
        {
            RegisterSource(source);
        }

        Logger.Debug("Initialized UpdateSourceFactory with {SourceCount} sources: {Sources}",
            _sources.Count, string.Join(", ", _sources.Keys));
    }

    public IUpdateSource? GetSource(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            Logger.Warning("Attempted to get update source with null or empty name");
            return null;
        }

        var found = _sources.TryGetValue(sourceName.Trim(), out var source);

        if (!found)
        {
            Logger.Warning("Update source '{SourceName}' not found. Available sources: {AvailableSources}",
                sourceName, string.Join(", ", _sources.Keys));
        }
        else
        {
            Logger.Debug("Retrieved update source: {SourceName}", source!.SourceName);
        }

        return source;
    }

    public IEnumerable<IUpdateSource> GetAllSources()
    {
        Logger.Debug("Returning all {SourceCount} update sources", _sources.Count);
        return _sources.Values;
    }

    public IEnumerable<IUpdateSource> GetPreReleaseCapableSources()
    {
        var preReleaseSources = _sources.Values.Where(s => s.SupportsPreReleases).ToList();
        Logger.Debug("Returning {PreReleaseSourceCount} pre-release capable sources: {Sources}",
            preReleaseSources.Count, string.Join(", ", preReleaseSources.Select(s => s.SourceName)));
        return preReleaseSources;
    }

    public void RegisterSource(IUpdateSource source)
    {
        if (source == null)
        {
            Logger.Warning("Attempted to register null update source");
            return;
        }

        if (string.IsNullOrWhiteSpace(source.SourceName))
        {
            Logger.Warning("Attempted to register update source with null or empty name");
            return;
        }

        if (_sources.ContainsKey(source.SourceName))
        {
            Logger.Warning("Update source '{SourceName}' is already registered, replacing with new instance",
                source.SourceName);
        }

        _sources[source.SourceName] = source;
        Logger.Debug("Registered update source: {SourceName} (SupportsPreReleases: {SupportsPreReleases})",
            source.SourceName, source.SupportsPreReleases);
    }
}
