using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace Classic.ScanLog.Validators;

/// <summary>
/// Validates texture files for proper format and dimensions
/// </summary>
public class TextureValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<TextureValidator> _logger;

    // DDS file header structure offsets
    private const int DDS_HEADER_SIZE = 20;
    private const int DDS_WIDTH_OFFSET = 12;
    private const int DDS_HEIGHT_OFFSET = 16;
    private static readonly byte[] DDS_SIGNATURE = { 0x44, 0x44, 0x53, 0x20 }; // "DDS "

    // Invalid texture formats that should be converted to DDS
    private static readonly HashSet<string> InvalidTextureFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tga", ".png", ".jpg", ".jpeg", ".bmp"
    };

    // Paths that are exempt from texture format validation
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "BodySlide", "OutfitStudio", "Tools", "Documentation", "ReadMe"
    };

    public TextureValidator(IFileSystem fileSystem, ILogger<TextureValidator> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Validates a texture file for format and dimensions
    /// </summary>
    public async Task<TextureValidationResult> ValidateTextureAsync(string filePath, string relativePath, CancellationToken cancellationToken = default)
    {
        var result = new TextureValidationResult
        {
            FilePath = filePath,
            RelativePath = relativePath,
            ValidationType = FileValidationType.Texture,
            Status = ValidationStatus.Valid
        };

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Check for invalid texture formats
            if (InvalidTextureFormats.Contains(extension))
            {
                // Check if path is exempt from validation
                if (IsExemptPath(relativePath))
                {
                    result.Status = ValidationStatus.Valid;
                    result.Description = $"Texture in exempt path: {extension.ToUpperInvariant()}";
                    return result;
                }

                result.Status = ValidationStatus.Warning;
                result.Issue = $"Invalid texture format: {extension.ToUpperInvariant()}";
                result.Description = "Texture files should be in DDS format for optimal game performance";
                result.Recommendation = "Convert texture to DDS format using a tool like Paint.NET with DDS plugin or Photoshop";
                result.Format = extension.ToUpperInvariant();
                return result;
            }

            // Validate DDS files
            if (extension == ".dds")
            {
                await ValidateDdsFileAsync(result, cancellationToken);
            }
            else
            {
                result.Description = $"Texture format: {extension.ToUpperInvariant()}";
                result.Format = extension.ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate texture file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating texture file: {FilePath}", filePath);
        }

        return result;
    }

    /// <summary>
    /// Validates DDS file header and dimensions
    /// </summary>
    private async Task ValidateDdsFileAsync(TextureValidationResult result, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(result.FilePath))
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "DDS file not found";
            return;
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(result.FilePath);
            var headerData = new byte[DDS_HEADER_SIZE];
            var bytesRead = await stream.ReadAsync(headerData, 0, DDS_HEADER_SIZE, cancellationToken);

            if (bytesRead < DDS_HEADER_SIZE)
            {
                result.Status = ValidationStatus.Error;
                result.Issue = "Invalid DDS file: Header too short";
                return;
            }

            // Verify DDS signature
            if (!headerData.Take(4).SequenceEqual(DDS_SIGNATURE))
            {
                result.Status = ValidationStatus.Error;
                result.Issue = "Invalid DDS file: Missing DDS signature";
                return;
            }

            // Extract dimensions
            result.Width = BitConverter.ToInt32(headerData, DDS_WIDTH_OFFSET);
            result.Height = BitConverter.ToInt32(headerData, DDS_HEIGHT_OFFSET);
            result.Format = "DDS";

            // Validate dimensions are power of 2
            if (!IsPowerOfTwo(result.Width) || !IsPowerOfTwo(result.Height))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = $"DDS dimensions not power of 2: {result.Width}x{result.Height}";
                result.Description = "Texture dimensions should be power of 2 (e.g., 512x512, 1024x1024) for optimal performance";
                result.Recommendation = "Resize texture to nearest power of 2 dimensions";
            }
            else
            {
                result.Description = $"Valid DDS texture: {result.Width}x{result.Height}";
            }

            result.Properties["Width"] = result.Width;
            result.Properties["Height"] = result.Height;
            result.Properties["IsPowerOfTwo"] = result.IsPowerOfTwo;
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to read DDS header";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error reading DDS file header: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Checks if a number is a power of 2
    /// </summary>
    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    /// Checks if the path is exempt from texture format validation
    /// </summary>
    private static bool IsExemptPath(string relativePath)
    {
        return ExemptPaths.Any(exemptPath => 
            relativePath.Contains(exemptPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates multiple texture files in parallel
    /// </summary>
    public async Task<List<TextureValidationResult>> ValidateTexturesAsync(
        IEnumerable<(string filePath, string relativePath)> textures, 
        CancellationToken cancellationToken = default)
    {
        var tasks = textures.Select(async texture =>
            await ValidateTextureAsync(texture.filePath, texture.relativePath, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }
}