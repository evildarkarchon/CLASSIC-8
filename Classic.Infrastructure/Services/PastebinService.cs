using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for fetching crash logs from Pastebin.
/// </summary>
public class PastebinService : IPastebinService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // Regex for validating Pastebin URLs and IDs
    private static readonly Regex PastebinUrlRegex =
        new(@"^https?://pastebin\.com/(\w+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PastebinIdRegex = new(@"^[a-zA-Z0-9]{8,}$", RegexOptions.Compiled);

    public PastebinService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CLASSIC-8 Crash Log Scanner");
    }

    public async Task<PastebinResult> FetchLogAsync(string urlOrId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return PastebinResult.CreateFailure(urlOrId, "URL or ID cannot be empty");
        }

        var trimmedInput = urlOrId.Trim();

        if (!IsValidPastebinReference(trimmedInput))
        {
            return PastebinResult.CreateFailure(trimmedInput, "Invalid Pastebin URL or ID format");
        }

        var rawUrl = GetRawUrl(trimmedInput);
        _logger.Information("Fetching Pastebin content from {RawUrl}", rawUrl);

        try
        {
            using var response = await _httpClient.GetAsync(rawUrl, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
                _logger.Warning("Failed to fetch Pastebin content: {ErrorMessage}", errorMessage);
                return PastebinResult.CreateFailure(trimmedInput, errorMessage);
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return PastebinResult.CreateFailure(trimmedInput, "Pastebin content is empty");
            }

            // Check if this looks like a crash log (basic validation)
            if (!IsLikelyCrashLog(content))
            {
                _logger.Warning("Downloaded content doesn't appear to be a crash log");
            }

            var filePath = await SaveContentToFileAsync(rawUrl, content, cancellationToken).ConfigureAwait(false);
            var contentSize = content.Length;

            _logger.Information("Successfully fetched Pastebin content: {ContentSize} bytes saved to {FilePath}",
                contentSize, filePath);

            return PastebinResult.CreateSuccess(trimmedInput, rawUrl, filePath, contentSize);
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Pastebin fetch operation was cancelled");
            return PastebinResult.CreateFailure(trimmedInput, "Operation was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Network error while fetching Pastebin content");
            return PastebinResult.CreateFailure(trimmedInput, $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while fetching Pastebin content");
            return PastebinResult.CreateFailure(trimmedInput, $"Unexpected error: {ex.Message}");
        }
    }

    public bool IsValidPastebinReference(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return false;
        }

        var trimmed = urlOrId.Trim();

        // Check if it's a valid Pastebin URL
        if (PastebinUrlRegex.IsMatch(trimmed))
        {
            return true;
        }

        // Check if it's a valid Pastebin ID
        if (PastebinIdRegex.IsMatch(trimmed))
        {
            return true;
        }

        return false;
    }

    public string GetRawUrl(string urlOrId)
    {
        var trimmed = urlOrId.Trim();

        // If it's already a URL, convert to raw
        var urlMatch = PastebinUrlRegex.Match(trimmed);
        if (urlMatch.Success)
        {
            var pastebinId = urlMatch.Groups[1].Value;
            return $"https://pastebin.com/raw/{pastebinId}";
        }

        // If it's just an ID, create the raw URL
        if (PastebinIdRegex.IsMatch(trimmed))
        {
            return $"https://pastebin.com/raw/{trimmed}";
        }

        // Fallback - assume it's an ID and try to make it work
        return $"https://pastebin.com/raw/{trimmed}";
    }

    private static bool IsLikelyCrashLog(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var lowerContent = content.ToLowerInvariant();

        // Look for common crash log indicators
        var crashIndicators = new[]
        {
            "exception",
            "crash",
            "error",
            "stack trace",
            "access violation",
            "unhandled exception",
            "fatal error",
            "assertion failed",
            "skyrim",
            "fallout",
            "bethesda",
            "game engine"
        };

        var indicatorCount = 0;
        foreach (var indicator in crashIndicators)
        {
            if (lowerContent.Contains(indicator))
            {
                indicatorCount++;
            }
        }

        // If we find multiple indicators, it's likely a crash log
        return indicatorCount >= 2;
    }

    private static async Task<string> SaveContentToFileAsync(string rawUrl, string content,
        CancellationToken cancellationToken)
    {
        // Create Pastebin directory if it doesn't exist
        var pastebinDirectory = Path.Combine("Crash Logs", "Pastebin");
        Directory.CreateDirectory(pastebinDirectory);

        // Extract the Pastebin ID from the raw URL
        var uri = new Uri(rawUrl);
        var pastebinId = Path.GetFileName(uri.AbsolutePath);

        // Create filename
        var fileName = $"crash-{pastebinId}.log";
        var filePath = Path.Combine(pastebinDirectory, fileName);

        // Handle filename conflicts
        var counter = 1;
        var originalFilePath = filePath;
        while (File.Exists(filePath))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
            var extension = Path.GetExtension(originalFilePath);
            var directory = Path.GetDirectoryName(originalFilePath) ?? "";

            fileName = $"{fileNameWithoutExtension}-{counter}{extension}";
            filePath = Path.Combine(directory, fileName);
            counter++;
        }

        // Save content to file
        await File.WriteAllTextAsync(filePath, content, cancellationToken).ConfigureAwait(false);

        return filePath;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
