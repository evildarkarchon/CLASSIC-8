using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public interface IPluginAnalyzer
{
    Task<object> AnalyzePluginsAsync(CrashLog crashLog, CancellationToken cancellationToken = default);
    Task<bool> HasPluginAsync(CrashLog crashLog, string pluginName, CancellationToken cancellationToken = default);
    Task<List<string>> ValidateLoadOrderAsync(CrashLog crashLog, CancellationToken cancellationToken = default);
}
