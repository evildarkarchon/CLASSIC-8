using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace Classic.ScanLog.Validators;

/// <summary>
/// Validates audio files for proper format and compatibility
/// </summary>
public class AudioValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AudioValidator> _logger;

    // Valid audio formats for Bethesda games
    private static readonly HashSet<string> ValidAudioFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".xwm", ".fuz"
    };

    // Invalid audio formats that need conversion
    private static readonly HashSet<string> InvalidAudioFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".m4a", ".ogg", ".flac", ".aac", ".wma"
    };

    // WAV file header constants
    private const int WAV_HEADER_SIZE = 44;
    private static readonly byte[] WAV_RIFF_SIGNATURE = System.Text.Encoding.ASCII.GetBytes("RIFF");
    private static readonly byte[] WAV_WAVE_SIGNATURE = System.Text.Encoding.ASCII.GetBytes("WAVE");

    public AudioValidator(IFileSystem fileSystem, ILogger<AudioValidator> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Validates an audio file for format and compatibility
    /// </summary>
    public async Task<AudioValidationResult> ValidateAudioAsync(string filePath, string relativePath, CancellationToken cancellationToken = default)
    {
        var result = new AudioValidationResult
        {
            FilePath = filePath,
            RelativePath = relativePath,
            ValidationType = FileValidationType.Audio,
            Status = ValidationStatus.Valid
        };

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            result.AudioFormat = extension.ToUpperInvariant();

            // Check for invalid audio formats
            if (InvalidAudioFormats.Contains(extension))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = $"Invalid audio format: {extension.ToUpperInvariant()}";
                result.Description = "Audio files should be in XWM or WAV format for game compatibility";
                result.Recommendation = GetFormatConversionRecommendation(extension);
                return result;
            }

            // Validate supported formats
            if (ValidAudioFormats.Contains(extension))
            {
                switch (extension)
                {
                    case ".wav":
                        await ValidateWavFileAsync(result, cancellationToken);
                        break;
                    case ".xwm":
                        await ValidateXwmFileAsync(result, cancellationToken);
                        break;
                    case ".fuz":
                        await ValidateFuzFileAsync(result, cancellationToken);
                        break;
                    default:
                        result.Description = $"Supported audio format: {extension.ToUpperInvariant()}";
                        break;
                }
            }
            else
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = $"Unknown audio format: {extension.ToUpperInvariant()}";
                result.Description = "File extension not recognized as a standard game audio format";
                result.Recommendation = "Verify that this is a valid audio file for the game";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate audio file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating audio file: {FilePath}", filePath);
        }

        return result;
    }

    /// <summary>
    /// Validates WAV file format and properties
    /// </summary>
    private async Task ValidateWavFileAsync(AudioValidationResult result, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(result.FilePath))
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "WAV file not found";
            return;
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(result.FilePath);
            var headerData = new byte[WAV_HEADER_SIZE];
            var bytesRead = await stream.ReadAsync(headerData, 0, WAV_HEADER_SIZE, cancellationToken);

            if (bytesRead < WAV_HEADER_SIZE)
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "WAV file header incomplete";
                result.Description = "File may be corrupted or not a valid WAV file";
                return;
            }

            // Verify RIFF signature
            if (!headerData.Take(4).SequenceEqual(WAV_RIFF_SIGNATURE))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "Invalid WAV file: Missing RIFF signature";
                return;
            }

            // Verify WAVE signature at offset 8
            if (!headerData.Skip(8).Take(4).SequenceEqual(WAV_WAVE_SIGNATURE))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "Invalid WAV file: Missing WAVE signature";
                return;
            }

            // Extract basic audio properties
            result.Channels = BitConverter.ToInt16(headerData, 22);
            result.SampleRate = BitConverter.ToInt32(headerData, 24);

            result.Description = $"Valid WAV file: {result.SampleRate}Hz, {result.Channels} channel(s)";
            result.Properties["SampleRate"] = result.SampleRate;
            result.Properties["Channels"] = result.Channels;

            // Check for optimal settings
            if (result.SampleRate > 48000)
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = $"High sample rate: {result.SampleRate}Hz";
                result.Recommendation = "Consider using 44.1kHz or 48kHz for better game compatibility";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to read WAV header";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error reading WAV file header: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Validates XWM file (Bethesda's compressed audio format)
    /// </summary>
    private async Task ValidateXwmFileAsync(AudioValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            // XWM files are Bethesda's proprietary format - basic validation
            var fileInfo = _fileSystem.FileInfo.New(result.FilePath);
            
            if (fileInfo.Length == 0)
            {
                result.Status = ValidationStatus.Error;
                result.Issue = "XWM file is empty";
                return;
            }

            result.Description = $"XWM audio file ({fileInfo.Length:N0} bytes)";
            result.Properties["FileSize"] = fileInfo.Length;

            // XWM is the preferred format for game audio
            if (result.Status == ValidationStatus.Valid)
            {
                result.Description += " - Optimal format for game performance";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate XWM file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating XWM file: {FilePath}", result.FilePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates FUZ file (Bethesda's voice format)
    /// </summary>
    private async Task ValidateFuzFileAsync(AudioValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = _fileSystem.FileInfo.New(result.FilePath);
            
            if (fileInfo.Length == 0)
            {
                result.Status = ValidationStatus.Error;
                result.Issue = "FUZ file is empty";
                return;
            }

            result.Description = $"FUZ voice file ({fileInfo.Length:N0} bytes)";
            result.Properties["FileSize"] = fileInfo.Length;
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate FUZ file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating FUZ file: {FilePath}", result.FilePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets format conversion recommendation based on the invalid format
    /// </summary>
    private static string GetFormatConversionRecommendation(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp3" => "Convert MP3 to XWM using Creation Kit or xWMAEncode tool",
            ".m4a" => "Convert M4A to XWM using Creation Kit or convert to WAV first",
            ".ogg" => "Convert OGG to XWM using Creation Kit or convert to WAV first",
            ".flac" => "Convert FLAC to WAV, then to XWM using Creation Kit",
            ".aac" => "Convert AAC to WAV, then to XWM using Creation Kit",
            ".wma" => "Convert WMA to WAV, then to XWM using Creation Kit",
            _ => "Convert to XWM format using Creation Kit or xWMAEncode tool"
        };
    }

    /// <summary>
    /// Validates multiple audio files in parallel
    /// </summary>
    public async Task<List<AudioValidationResult>> ValidateAudioFilesAsync(
        IEnumerable<(string filePath, string relativePath)> audioFiles, 
        CancellationToken cancellationToken = default)
    {
        var tasks = audioFiles.Select(async audio =>
            await ValidateAudioAsync(audio.filePath, audio.relativePath, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// Checks if a file is an audio format (valid or invalid)
    /// </summary>
    public static bool IsAudioFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return ValidAudioFormats.Contains(extension) || InvalidAudioFormats.Contains(extension);
    }
}