using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Classic.ScanLog.Validators;

/// <summary>
/// Validates script files for conflicts and integrity
/// </summary>
public class ScriptValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ScriptValidator> _logger;

    // Script file extensions
    private static readonly HashSet<string> ScriptExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pex", ".psc"
    };

    // XSE script files that shouldn't be included in mods (will be loaded from configuration)
    private static readonly Dictionary<string, string> KnownXseScripts = new(StringComparer.OrdinalIgnoreCase)
    {
        // F4SE scripts
        { "f4se.pex", "F4SE Script Extender" },
        { "f4se_loader.pex", "F4SE Loader" },
        { "workshopframework.pex", "Workshop Framework" },
        
        // SKSE scripts  
        { "skse.pex", "SKSE Script Extender" },
        { "skse_loader.pex", "SKSE Loader" },
        
        // Common framework scripts
        { "mcm.pex", "Mod Configuration Menu" },
        { "skyui.pex", "SkyUI Framework" }
    };

    // Paths that are exempt from XSE script validation
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "Workshop Framework", "MCM", "SkyUI", "F4SE", "SKSE", "Data\\Scripts\\Source"
    };

    public ScriptValidator(IFileSystem fileSystem, ILogger<ScriptValidator> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Validates a script file for conflicts and integrity
    /// </summary>
    public async Task<ScriptValidationResult> ValidateScriptAsync(string filePath, string relativePath, CancellationToken cancellationToken = default)
    {
        var result = new ScriptValidationResult
        {
            FilePath = filePath,
            RelativePath = relativePath,
            ValidationType = FileValidationType.Script,
            Status = ValidationStatus.Valid
        };

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(filePath);
            
            result.ScriptType = extension.ToUpperInvariant();

            // Check for XSE script conflicts
            if (ScriptExtensions.Contains(extension))
            {
                await ValidateScriptFileAsync(result, fileName, cancellationToken);
            }
            else
            {
                result.Description = $"Script file: {extension.ToUpperInvariant()}";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate script file";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating script file: {FilePath}", filePath);
        }

        return result;
    }

    /// <summary>
    /// Validates script file for XSE conflicts and integrity
    /// </summary>
    private async Task ValidateScriptFileAsync(ScriptValidationResult result, string fileName, CancellationToken cancellationToken)
    {
        // Check if this is a known XSE script
        if (KnownXseScripts.TryGetValue(fileName, out var scriptName))
        {
            // Check if path is exempt from validation
            if (IsExemptPath(result.RelativePath))
            {
                result.Description = $"XSE script in exempt path: {scriptName}";
                result.IsXseScript = true;
                return;
            }

            result.Status = ValidationStatus.Warning;
            result.Issue = $"XSE script conflict: {fileName}";
            result.Description = $"This mod contains a copy of {scriptName} which may conflict with the script extender";
            result.Recommendation = "Remove this script file from the mod or ensure it's the correct version";
            result.IsXseScript = true;
            result.ConflictingMod = scriptName;
            return;
        }

        // For regular script files, check basic integrity
        await ValidateScriptIntegrityAsync(result, cancellationToken);
    }

    /// <summary>
    /// Validates script file integrity and basic properties
    /// </summary>
    private async Task ValidateScriptIntegrityAsync(ScriptValidationResult result, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(result.FilePath))
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Script file not found";
            return;
        }

        try
        {
            var fileInfo = _fileSystem.FileInfo.New(result.FilePath);
            
            if (fileInfo.Length == 0)
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "Script file is empty";
                result.Description = "Empty script files may cause issues";
                return;
            }

            // Calculate file hash for integrity checking
            result.ActualHash = await CalculateFileHashAsync(result.FilePath, cancellationToken);
            result.Properties["FileSize"] = fileInfo.Length;
            result.Properties["FileHash"] = result.ActualHash;

            var extension = Path.GetExtension(result.FilePath).ToLowerInvariant();
            
            if (extension == ".pex")
            {
                await ValidatePexFileAsync(result, cancellationToken);
            }
            else if (extension == ".psc")
            {
                await ValidatePscFileAsync(result, cancellationToken);
            }
            else
            {
                result.Description = $"Script file: {fileInfo.Length:N0} bytes";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Error;
            result.Issue = "Failed to validate script integrity";
            result.Description = ex.Message;
            _logger.LogError(ex, "Error validating script integrity: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Validates compiled Papyrus script (.pex) file
    /// </summary>
    private async Task ValidatePexFileAsync(ScriptValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = _fileSystem.File.OpenRead(result.FilePath);
            var headerData = new byte[8];
            var bytesRead = await stream.ReadAsync(headerData, 0, 8, cancellationToken);

            if (bytesRead >= 4)
            {
                // Check for basic PEX file structure (simplified validation)
                var header = BitConverter.ToUInt32(headerData, 0);
                
                // PEX files typically start with specific magic numbers
                result.Description = $"Compiled Papyrus script ({result.Properties["FileSize"]:N0} bytes)";
                result.Properties["PexHeader"] = Convert.ToHexString(headerData);
            }
            else
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "PEX file too small or corrupted";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Warning;
            result.Issue = "Failed to read PEX file header";
            result.Description = ex.Message;
            _logger.LogWarning(ex, "Error reading PEX file: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Validates Papyrus source code (.psc) file
    /// </summary>
    private async Task ValidatePscFileAsync(ScriptValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _fileSystem.File.ReadAllTextAsync(result.FilePath, cancellationToken);
            
            // Basic PSC file validation
            if (string.IsNullOrWhiteSpace(content))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "PSC file is empty or contains only whitespace";
                return;
            }

            // Count lines for basic metrics
            var lineCount = content.Split('\n').Length;
            result.Properties["LineCount"] = lineCount;
            
            result.Description = $"Papyrus source code ({lineCount:N0} lines, {result.Properties["FileSize"]:N0} bytes)";

            // Check for basic script structure
            if (!content.Contains("ScriptName", StringComparison.OrdinalIgnoreCase))
            {
                result.Status = ValidationStatus.Warning;
                result.Issue = "PSC file missing ScriptName declaration";
                result.Recommendation = "Verify this is a valid Papyrus script file";
            }
        }
        catch (Exception ex)
        {
            result.Status = ValidationStatus.Warning;
            result.Issue = "Failed to read PSC file content";
            result.Description = ex.Message;
            _logger.LogWarning(ex, "Error reading PSC file: {FilePath}", result.FilePath);
        }
    }

    /// <summary>
    /// Calculates SHA-256 hash of a file
    /// </summary>
    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = _fileSystem.File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate hash for file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if the path is exempt from XSE script validation
    /// </summary>
    private static bool IsExemptPath(string relativePath)
    {
        return ExemptPaths.Any(exemptPath => 
            relativePath.Contains(exemptPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates multiple script files in parallel
    /// </summary>
    public async Task<List<ScriptValidationResult>> ValidateScriptsAsync(
        IEnumerable<(string filePath, string relativePath)> scripts, 
        CancellationToken cancellationToken = default)
    {
        var tasks = scripts.Select(async script =>
            await ValidateScriptAsync(script.filePath, script.relativePath, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// Checks if a file is a script file
    /// </summary>
    public static bool IsScriptFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return ScriptExtensions.Contains(extension);
    }
}