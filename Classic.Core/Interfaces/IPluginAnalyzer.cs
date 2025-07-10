using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public interface IPluginAnalyzer
{
    Task<List<Plugin>> ExtractPluginsAsync(CrashLog crashLog, CancellationToken cancellationToken = default);
    Task<List<Plugin>> AnalyzePluginConflictsAsync(List<Plugin> plugins, CancellationToken cancellationToken = default);
    Task<bool> IsKnownProblematicPluginAsync(Plugin plugin, CancellationToken cancellationToken = default);
}
