using System;

namespace Classic.Core.Models;

/// <summary>
/// Result of a Pastebin fetch operation.
/// </summary>
public class PastebinResult
{
    /// <summary>
    /// Whether the fetch operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The original URL that was fetched.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// The raw URL that was actually used for fetching.
    /// </summary>
    public string RawUrl { get; init; } = string.Empty;

    /// <summary>
    /// The file path where the content was saved.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The size of the downloaded content in bytes.
    /// </summary>
    public long ContentSize { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PastebinResult CreateSuccess(string url, string rawUrl, string filePath, long contentSize)
    {
        return new PastebinResult
        {
            Success = true,
            Url = url,
            RawUrl = rawUrl,
            FilePath = filePath,
            ContentSize = contentSize
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PastebinResult CreateFailure(string url, string errorMessage)
    {
        return new PastebinResult
        {
            Success = false,
            Url = url,
            ErrorMessage = errorMessage
        };
    }
}
