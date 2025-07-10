using System.IO.Abstractions;
using System.Text;
using Serilog;
using Tommy;
using Ude;

namespace Classic.ScanGame.Configuration;

/// <summary>
/// Manages TOML configuration files with read/write capabilities.
/// </summary>
public class TomlConfigurationManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public TomlConfigurationManager(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Gets a setting value from a TOML file.
    /// </summary>
    public async Task<object?> GetSettingAsync(string filePath, string section, string key)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            using var reader = new StringReader(content);
            var document = TOML.Parse(reader);

            if (!document.HasKey(section))
            {
                return null;
            }

            var sectionTable = document[section];
            if (sectionTable == null || !sectionTable.HasKey(key))
            {
                return null;
            }

            var value = sectionTable[key];
            return ConvertTomlValue(value);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get setting {Section}.{Key} from {FilePath}", section, key, filePath);
            return null;
        }
    }

    /// <summary>
    /// Sets a setting value in a TOML file.
    /// </summary>
    public async Task SetSettingAsync(string filePath, string section, string key, object value)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            using var reader = new StringReader(content);
            var document = TOML.Parse(reader);

            // Ensure section exists
            if (!document.HasKey(section))
            {
                document[section] = new TomlTable();
            }

            var sectionTable = document[section];
            if (sectionTable == null)
            {
                throw new InvalidOperationException($"Section '{section}' is not a table");
            }

            // Set the value
            sectionTable[key] = ConvertToTomlValue(value);

            // Write back to file
            using var writer = new StringWriter();
            document.WriteTo(writer);
            var newContent = writer.ToString();
            await WriteFileWithEncodingAsync(filePath, newContent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set setting {Section}.{Key} = {Value} in {FilePath}", section, key, value, filePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a setting exists in a TOML file.
    /// </summary>
    public async Task<bool> HasSettingAsync(string filePath, string section, string key)
    {
        try
        {
            var content = await ReadFileWithEncodingAsync(filePath);
            using var reader = new StringReader(content);
            var document = TOML.Parse(reader);

            if (!document.HasKey(section))
            {
                return false;
            }

            var sectionTable = document[section];
            return sectionTable?.HasKey(key) == true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to check setting {Section}.{Key} in {FilePath}", section, key, filePath);
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
    /// Converts a TOML value to a .NET object.
    /// </summary>
    private static object? ConvertTomlValue(TomlNode value)
    {
        if (value.IsString) return value.AsString?.Value;
        if (value.IsBoolean) return value.AsBoolean?.Value;
        if (value.IsInteger) return value.AsInteger?.Value;
        if (value.IsFloat) return value.AsFloat?.Value;
        if (value.IsDateTime)
        {
            // Handle different datetime types
            if (value is TomlDateTimeLocal dtLocal) return dtLocal.Value;
            if (value is TomlDateTimeOffset dtOffset) return dtOffset.Value;
        }
        if (value.IsArray)
        {
            var array = value.AsArray;
            var result = new List<object?>();
            for (int i = 0; i < array.RawArray.Count; i++)
            {
                result.Add(ConvertTomlValue(array[i]));
            }
            return result.ToArray();
        }
        if (value.IsTable)
        {
            var table = value.AsTable;
            var result = new Dictionary<string, object?>();
            foreach (var kvp in table.RawTable)
            {
                result[kvp.Key] = ConvertTomlValue(kvp.Value);
            }
            return result;
        }
        return value?.ToString();
    }

    /// <summary>
    /// Converts a .NET object to a TOML value.
    /// </summary>
    private static TomlNode ConvertToTomlValue(object value)
    {
        return value switch
        {
            string s => s,
            bool b => b,
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            DateTime dt => dt,
            _ => value.ToString() ?? string.Empty
        };
    }
}