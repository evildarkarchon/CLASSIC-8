using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;
using Ude;

namespace Classic.ScanGame.Configuration;

/// <summary>
/// Manages INI configuration files with read/write capabilities.
/// </summary>
public class IniConfigurationManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public IniConfigurationManager(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Gets a setting value from an INI file.
    /// </summary>
    public async Task<T?> GetSettingAsync<T>(string filePath, string section, string key)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            var config = ParseIniConfiguration(content);
            
            var value = config.GetSection($"{section}:{key}").Value;
            if (value == null)
            {
                return default;
            }

            return (T?)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get setting {Section}:{Key} from {FilePath}", section, key, filePath);
            return default;
        }
    }

    /// <summary>
    /// Sets a setting value in an INI file.
    /// </summary>
    public async Task SetSettingAsync(string filePath, string section, string key, object value)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            var lines = content.Split('\n').ToList();
            
            var sectionIndex = FindSectionIndex(lines, section);
            var keyIndex = FindKeyIndex(lines, section, key, sectionIndex);

            var valueString = ConvertValueToString(value);

            if (keyIndex >= 0)
            {
                // Update existing key
                lines[keyIndex] = $"{key}={valueString}";
            }
            else if (sectionIndex >= 0)
            {
                // Add key to existing section
                var insertIndex = FindSectionEndIndex(lines, sectionIndex);
                lines.Insert(insertIndex, $"{key}={valueString}");
            }
            else
            {
                // Add new section and key
                lines.Add($"[{section}]");
                lines.Add($"{key}={valueString}");
            }

            var newContent = string.Join('\n', lines);
            await WriteFileWithEncodingAsync(filePath, newContent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set setting {Section}:{Key} = {Value} in {FilePath}", section, key, value, filePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a setting exists in an INI file.
    /// </summary>
    public async Task<bool> HasSettingAsync(string filePath, string section, string key)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            var config = ParseIniConfiguration(content);
            return config.GetSection($"{section}:{key}").Exists();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to check setting {Section}:{Key} in {FilePath}", section, key, filePath);
            return false;
        }
    }

    /// <summary>
    /// Reads a file with automatic encoding detection.
    /// </summary>
    private async Task<string> ReadFileWithEncodingAsync(string filePath)
    {
        var fileBytes = await _fileSystem.File.ReadAllBytesAsync(filePath);
        
        // Detect encoding
        var detector = new CharsetDetector();
        detector.Feed(fileBytes, 0, fileBytes.Length);
        detector.DataEnd();

        var encoding = Encoding.UTF8; // Default fallback
        if (detector.Charset != null)
        {
            try
            {
                encoding = Encoding.GetEncoding(detector.Charset);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get encoding {Charset}, using UTF-8", detector.Charset);
            }
        }

        return encoding.GetString(fileBytes);
    }

    /// <summary>
    /// Writes a file with encoding detection.
    /// </summary>
    private async Task WriteFileWithEncodingAsync(string filePath, string content)
    {
        try
        {
            // Try to detect the original encoding
            var originalBytes = await _fileSystem.File.ReadAllBytesAsync(filePath);
            var detector = new CharsetDetector();
            detector.Feed(originalBytes, 0, originalBytes.Length);
            detector.DataEnd();

            var encoding = Encoding.UTF8; // Default fallback
            if (detector.Charset != null)
            {
                try
                {
                    encoding = Encoding.GetEncoding(detector.Charset);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to get encoding {Charset}, using UTF-8", detector.Charset);
                }
            }

            var contentBytes = encoding.GetBytes(content);
            await _fileSystem.File.WriteAllBytesAsync(filePath, contentBytes);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to write file with detected encoding, using UTF-8");
            await _fileSystem.File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Parses INI file content into a configuration object.
    /// </summary>
    private IConfigurationRoot ParseIniConfiguration(string content)
    {
        var data = new Dictionary<string, string?>();
        var lines = content.Split('\n');
        string? currentSection = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(';') || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1];
                continue;
            }

            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0 && !string.IsNullOrEmpty(currentSection))
            {
                var key = trimmedLine[..equalIndex].Trim();
                var value = trimmedLine[(equalIndex + 1)..].Trim();
                data[$"{currentSection}:{key}"] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    /// <summary>
    /// Finds the index of a section in the lines array.
    /// </summary>
    private static int FindSectionIndex(List<string> lines, string section)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].Trim();
            if (trimmedLine.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds the index of a key within a section.
    /// </summary>
    private static int FindKeyIndex(List<string> lines, string section, string key, int sectionIndex)
    {
        if (sectionIndex < 0)
        {
            return -1;
        }

        for (var i = sectionIndex + 1; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].Trim();
            
            // Stop if we hit another section
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                break;
            }

            // Check if this line contains our key
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0)
            {
                var lineKey = trimmedLine[..equalIndex].Trim();
                if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the end index of a section (where to insert new keys).
    /// </summary>
    private static int FindSectionEndIndex(List<string> lines, int sectionIndex)
    {
        for (var i = sectionIndex + 1; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].Trim();
            
            // Stop if we hit another section
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                return i;
            }
        }

        return lines.Count;
    }

    /// <summary>
    /// Converts a value to its string representation for INI files.
    /// </summary>
    private static string ConvertValueToString(object value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? string.Empty
        };
    }
}