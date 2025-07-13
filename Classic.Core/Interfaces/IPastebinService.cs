using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for fetching crash logs from Pastebin.
/// </summary>
public interface IPastebinService
{
    /// <summary>
    /// Fetches a crash log from Pastebin and saves it locally.
    /// </summary>
    /// <param name="urlOrId">Pastebin URL or ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the fetch operation</returns>
    Task<PastebinResult> FetchLogAsync(string urlOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a string is a valid Pastebin URL or ID.
    /// </summary>
    /// <param name="urlOrId">String to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidPastebinReference(string urlOrId);

    /// <summary>
    /// Converts a Pastebin URL or ID to the raw content URL.
    /// </summary>
    /// <param name="urlOrId">Pastebin URL or ID</param>
    /// <returns>Raw content URL</returns>
    string GetRawUrl(string urlOrId);
}
