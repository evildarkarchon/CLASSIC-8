using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public interface IFormIDAnalyzer
{
    Task<List<FormID>> ExtractFormIDsAsync(CrashLog crashLog, CancellationToken cancellationToken = default);
    Task<FormID?> AnalyzeFormIDAsync(string formIdString, CancellationToken cancellationToken = default);
    Task<bool> IsKnownProblematicFormIDAsync(FormID formId, CancellationToken cancellationToken = default);
}
