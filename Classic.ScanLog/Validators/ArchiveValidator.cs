using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Text;

namespace Classic.ScanLog.Validators;

/// <summary>
/// Validates BA2 archive files for proper format and structure
/// </summary>
public class ArchiveValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ArchiveValidator> _logger;

    // BA2 file header constants
    private const int BA2_HEADER_SIZE = 12;
    private static readonly byte[] BA2_SIGNATURE = Encoding.ASCII.GetBytes("BTDX"); // BA2 signature
    private static readonly byte[] DX10_FORMAT = Encoding.ASCII.GetBytes("DX10"); // Texture format
    private static readonly byte[] GNRL_FORMAT = Encoding.ASCII.GetBytes("GNRL"); // General format

    public ArchiveValidator(IFileSystem fileSystem, ILogger<ArchiveValidator> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Validates a BA2 archive file
    /// </summary>
    public async Task<ArchiveValidationResult> ValidateArchiveAsync(string filePath, string relativePath,
        CancellationToken cancellationToken = default)
    {
        var result = new ArchiveValidationResult
        {
            FilePath = filePath,
            RelativePath = relativePath,
            ValidationType = FileValidationType.Archive,
            Status = ValidationStatus.Valid
        };

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".ba2")
            {
                await ValidateBa2FileAsync(result, cancellationToken);
            }
            else
            {
                result.Description = $"Archive format: {extension.ToUpperInvariant()}";
                result.ArchiveFormat = extension.ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate archive file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating archive file: {FilePath}", filePath);
        }

        return result;
    }

    /// <summary>
    /// Validates BA2 file header and format
    /// </summary>
    private async Task ValidateBa2FileAsync(ArchiveValidationResult result, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(result.FilePath))
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "BA2 file not found";
            return;
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(result.FilePath);
            var headerData = new byte[BA2_HEADER_SIZE];
            var bytesRead = await stream.ReadAsync(headerData, 0, BA2_HEADER_SIZE, cancellationToken);

            if (bytesRead < BA2_HEADER_SIZE)
            {
                result.Status = ValidationStatus.Error;
                result.Issue = "Invalid BA2 file: Header too short";
                return;
            }

            // Store header for debugging
            result.Header = Convert.ToHexString(headerData);

            // Verify BA2 signature (BTDX)
            if (!headerData.Take(4).SequenceEqual(BA2_SIGNATURE))
            {
                result.Status = ValidationStatus.Error;
                result.Issue =
                    $"Invalid BA2 file: Invalid signature. Expected BTDX, got {Encoding.ASCII.GetString(headerData, 0, 4)}";
                result.Description = "BA2 files must start with BTDX signature";
                return;
            }

            // Check format type at offset 8
            var formatBytes = headerData.Skip(8).Take(4).ToArray();
            var formatString = Encoding.ASCII.GetString(formatBytes);

            if (formatBytes.SequenceEqual(DX10_FORMAT))
            {
                result.ArchiveFormat = "BTDX-DX10";
                result.Description = "Valid texture BA2 archive (DX10 format)";
                result.Properties["ArchiveType"] = "Texture";

                // For texture archives, we could analyze texture contents if needed
                await AnalyzeTextureBa2Async(result, stream, cancellationToken);
            }
            else if (formatBytes.SequenceEqual(GNRL_FORMAT))
            {
                result.ArchiveFormat = "BTDX-GNRL";
                result.Description = "Valid general BA2 archive (GNRL format)";
                result.Properties["ArchiveType"] = "General";

                // For general archives, we could analyze file contents if needed
                await AnalyzeGeneralBa2Async(result, stream, cancellationToken);
            }
            else
            {
                result.Status = ValidationStatus.Error;
                result.Issue = $"Invalid BA2 format: {formatString}";
                result.Description = "BA2 archives must be either BTDX-DX10 (texture) or BTDX-GNRL (general) format";
                result.Recommendation = "Recreate the BA2 archive with proper format using Creation Kit or Archive2";
                return;
            }

            // Get file size
            var fileInfo = _fileSystem.FileInfo.New(result.FilePath);
            result.TotalSize = fileInfo.Length;
            result.Properties["FileSize"] = result.TotalSize;
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to read BA2 header";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error reading BA2 file header: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Analyzes texture BA2 archive contents (DX10 format)
    /// </summary>
    private async Task AnalyzeTextureBa2Async(ArchiveValidationResult result, Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            // For now, just count the approximate number of files based on file size
            // A full implementation would parse the BA2 internal structure
            var estimatedFileCount = result.TotalSize / (1024 * 100); // Rough estimate based on average texture size
            result.FileCount = Math.Max(1, estimatedFileCount);
            result.Properties["EstimatedFileCount"] = result.FileCount;

            _logger.LogDebug("Analyzed texture BA2: {FilePath}, estimated {FileCount} files", result.FilePath,
                result.FileCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze texture BA2 contents: {FilePath}", result.FilePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Analyzes general BA2 archive contents (GNRL format)
    /// </summary>
    private async Task AnalyzeGeneralBa2Async(ArchiveValidationResult result, Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            // For now, just estimate file count based on size
            // A full implementation would parse the BA2 internal structure
            var estimatedFileCount = result.TotalSize / (1024 * 50); // Rough estimate based on average file size
            result.FileCount = Math.Max(1, estimatedFileCount);
            result.Properties["EstimatedFileCount"] = result.FileCount;

            _logger.LogDebug("Analyzed general BA2: {FilePath}, estimated {FileCount} files", result.FilePath,
                result.FileCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze general BA2 contents: {FilePath}", result.FilePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates multiple archive files in parallel
    /// </summary>
    public async Task<List<ArchiveValidationResult>> ValidateArchivesAsync(
        IEnumerable<(string filePath, string relativePath)> archives,
        CancellationToken cancellationToken = default)
    {
        var tasks = archives.Select(async archive =>
            await ValidateArchiveAsync(archive.filePath, archive.relativePath, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// Checks if a file is a supported archive format
    /// </summary>
    public static bool IsSupportedArchive(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".ba2";
    }
}
