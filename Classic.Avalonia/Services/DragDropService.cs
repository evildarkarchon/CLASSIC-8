using Avalonia.Input;
using Avalonia.Platform.Storage;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using NotificationType = Classic.Core.Models.NotificationType;

namespace Classic.Avalonia.Services;

/// <summary>
/// Interface for drag and drop file handling services.
/// </summary>
public interface IDragDropService
{
    /// <summary>
    /// Validates if the dragged files are crash logs.
    /// </summary>
    /// <param name="e">Drag event args</param>
    /// <returns>True if valid crash log files are being dragged</returns>
    bool ValidateDropData(DragEventArgs e);

    /// <summary>
    /// Processes dropped crash log files.
    /// </summary>
    /// <param name="e">Drop event args</param>
    /// <returns>List of valid crash log file paths</returns>
    Task<IEnumerable<string>> ProcessDroppedFilesAsync(DragEventArgs e);

    /// <summary>
    /// Event fired when valid files are dropped.
    /// </summary>
    event EventHandler<FilesDroppedEventArgs>? FilesDropped;
}

/// <summary>
/// Event arguments for files dropped events.
/// </summary>
public class FilesDroppedEventArgs : EventArgs
{
    public IEnumerable<string> FilePaths { get; }
    public int FileCount => FilePaths.Count();

    public FilesDroppedEventArgs(IEnumerable<string> filePaths)
    {
        FilePaths = filePaths;
    }
}

/// <summary>
/// Service for handling drag and drop operations for crash log files.
/// </summary>
public class DragDropService : IDragDropService
{
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;

    // Valid crash log file extensions
    private static readonly string[] ValidExtensions = { ".log", ".dmp", ".txt" };

    // Valid crash log file name patterns
    private static readonly string[] ValidPatterns = { "crash", "dump", "exception", "fatal" };

    public event EventHandler<FilesDroppedEventArgs>? FilesDropped;

    public DragDropService(ILogger logger, INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public bool ValidateDropData(DragEventArgs e)
    {
        try
        {
            if (!e.Data.Contains(DataFormats.Files)) return false;

            var files = e.Data.GetFiles();
            if (files == null || !files.Any()) return false;

            // Check if any of the files could be crash logs
            return files.Any(IsValidCrashLogFile);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating drop data");
            return false;
        }
    }

    public async Task<IEnumerable<string>> ProcessDroppedFilesAsync(DragEventArgs e)
    {
        var validFiles = new List<string>();

        try
        {
            if (!e.Data.Contains(DataFormats.Files)) return validFiles;

            var files = e.Data.GetFiles();
            if (files == null) return validFiles;

            foreach (var file in files)
            {
                var filePath = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(filePath)) continue;

                // Handle directories by scanning for crash logs
                if (Directory.Exists(filePath))
                {
                    var foundFiles = await ScanDirectoryForCrashLogsAsync(filePath);
                    validFiles.AddRange(foundFiles);
                }
                // Handle individual files
                else if (File.Exists(filePath) && IsValidCrashLogFile(file))
                {
                    validFiles.Add(filePath);
                }
            }

            if (validFiles.Any())
            {
                FilesDropped?.Invoke(this, new FilesDroppedEventArgs(validFiles));

                await _notificationService.ShowNotificationAsync(
                    "Files Dropped",
                    $"Found {validFiles.Count} crash log file(s) ready for scanning.",
                    NotificationType.Information);

                _logger.Information("Processed {Count} dropped crash log files", validFiles.Count);
            }
            else
            {
                await _notificationService.ShowNotificationAsync(
                    "No Valid Files",
                    "No valid crash log files were found in the dropped items.",
                    NotificationType.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing dropped files");
            await _notificationService.ShowNotificationAsync(
                "Drop Error",
                "An error occurred while processing the dropped files.",
                NotificationType.Error);
        }

        return validFiles;
    }

    private static bool IsValidCrashLogFile(IStorageItem file)
    {
        var fileName = file.Name.ToLowerInvariant();

        // Check file extension
        var hasValidExtension = ValidExtensions.Any(ext => fileName.EndsWith(ext));
        if (!hasValidExtension) return false;

        // Check if filename contains crash-related patterns
        var hasValidPattern = ValidPatterns.Any(pattern => fileName.Contains(pattern));

        return hasValidPattern;
    }

    private async Task<IEnumerable<string>> ScanDirectoryForCrashLogsAsync(string directoryPath)
    {
        var crashLogs = new List<string>();

        try
        {
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();

                // Check extension and pattern matching
                var hasValidExtension = ValidExtensions.Any(ext => fileName.EndsWith(ext));
                var hasValidPattern = ValidPatterns.Any(pattern => fileName.Contains(pattern));

                if (hasValidExtension && hasValidPattern) crashLogs.Add(file);
            }

            _logger.Debug("Found {Count} crash log files in directory: {Directory}", crashLogs.Count, directoryPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scanning directory for crash logs: {Directory}", directoryPath);
        }

        await Task.CompletedTask;
        return crashLogs;
    }
}
