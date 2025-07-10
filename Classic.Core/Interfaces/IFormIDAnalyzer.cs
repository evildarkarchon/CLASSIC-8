using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public interface IFormIdAnalyzer
{
    Task<List<FormId>> AnalyzeFormIDsAsync(CrashLog crashLog, CancellationToken cancellationToken = default);
    Task<FormId> ResolveFormIdAsync(string formIdString, CancellationToken cancellationToken = default);

    Task<List<string>> ValidateFormIDsAsync(CrashLog crashLog, List<FormId> formIDs,
        CancellationToken cancellationToken = default);
}
