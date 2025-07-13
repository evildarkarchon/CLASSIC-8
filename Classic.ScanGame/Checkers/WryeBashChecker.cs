using System.IO.Abstractions;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Classic.Core.Interfaces;
using Classic.Infrastructure;
using Serilog;
using Ude;

namespace Classic.ScanGame.Checkers;

/// <summary>
/// Scans the Wrye Bash plugin checker report for detected problems.
/// </summary>
public class WryeBashChecker : IWryeBashChecker
{
    private readonly IFileSystem _fileSystem;
    private readonly IYamlSettingsCache _yamlSettings;
    private readonly IGameConfiguration _gameConfiguration;
    private readonly ILogger _logger;

    private static readonly Dictionary<string, string> ResourceLinks = new()
    {
        ["troubleshooting"] = "https://www.nexusmods.com/fallout4/articles/4141",
        ["documentation"] = "https://wrye-bash.github.io/docs/",
        ["simple_eslify"] = "https://www.nexusmods.com/skyrimspecialedition/mods/27568"
    };

    public WryeBashChecker(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGameConfiguration gameConfiguration,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _yamlSettings = yamlSettings;
        _gameConfiguration = gameConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Scans game files and configurations for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    public async Task<string> ScanAsync()
    {
        return await ScanWryeCheckAsync();
    }

    /// <summary>
    /// Scans the Wrye Bash plugin checker report for detected problems and generates a detailed analysis message.
    /// </summary>
    /// <returns>The generated analysis message detailing the contents of the plugin checker report.</returns>
    public async Task<string> ScanWryeCheckAsync()
    {
        try
        {
            // Load settings from YAML
            var missingHtmlSetting =
                await _yamlSettings.GetSettingAsync<string>("Game", "Warnings_MODS.Warn_WRYE_MissingHTML");
            var vrSuffix = _gameConfiguration.IsVrMode ? "VR" : "";
            var pluginCheckPath =
                await _yamlSettings.GetSettingAsync<string>("Game_Local", $"Game{vrSuffix}_Info.Docs_File_WryeBashPC");
            var wryeWarnings =
                await _yamlSettings.GetSettingAsync<Dictionary<string, string>>("Main", "Warnings_WRYE") ??
                new Dictionary<string, string>();

            // Return early if report not found
            if (string.IsNullOrEmpty(pluginCheckPath) || !_fileSystem.File.Exists(pluginCheckPath))
            {
                if (!string.IsNullOrEmpty(missingHtmlSetting)) return missingHtmlSetting;
                throw new InvalidOperationException("ERROR: Warnings_WRYE missing from the database!");
            }

            // Build the message
            var messageParts = new List<string>
            {
                "\n✔️ WRYE BASH PLUGIN CHECKER REPORT WAS FOUND! ANALYZING CONTENTS...\n",
                $"  [This report is located in your Documents/My Games/{_gameConfiguration.CurrentGame} folder.]\n",
                "  [To hide this report, remove *ModChecker.html* from the same folder.]\n"
            };

            // Parse the HTML report
            var reportContents = await ParseWryeReportAsync(pluginCheckPath, wryeWarnings);
            messageParts.AddRange(reportContents);

            // Add resource links
            messageParts.AddRange(new[]
            {
                "\n❔ For more info about the above detected problems, see the WB Advanced Readme\n",
                "  For more details about solutions, read the Advanced Troubleshooting Article\n",
                $"  Advanced Troubleshooting: {ResourceLinks["troubleshooting"]}\n",
                $"  Wrye Bash Advanced Readme Documentation: {ResourceLinks["documentation"]}\n",
                "  [ After resolving any problems, run Plugin Checker in Wrye Bash again! ]\n\n"
            });

            return string.Join("", messageParts);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scanning Wrye Bash check");
            return "❌ ERROR: Failed to scan Wrye Bash plugin checker report\n-----\n";
        }
    }

    /// <summary>
    /// Parses a Wrye Bash report in HTML format and extracts relevant messages and plugin details.
    /// </summary>
    /// <param name="reportPath">The path to the Wrye Bash report file in HTML format.</param>
    /// <param name="wryeWarnings">A dictionary mapping warning names to their respective warning messages.</param>
    /// <returns>A list of formatted message strings containing details from the report and warnings.</returns>
    private async Task<List<string>> ParseWryeReportAsync(string reportPath, Dictionary<string, string> wryeWarnings)
    {
        var messageParts = new List<string>();

        try
        {
            // Read and parse HTML file
            var htmlContent = await ReadFileWithEncodingAsync(reportPath);
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(htmlContent));

            // Process each section (h3 element)
            var sections = document.QuerySelectorAll("h3");
            foreach (var section in sections.OfType<IHtmlHeadingElement>())
            {
                var title = section.TextContent?.Trim() ?? "";
                var plugins = ExtractPluginsFromSection(section);

                // Format section header
                if (title != "Active Plugins:") messageParts.Add(FormatSectionHeader(title));

                // Handle special ESL Capable section
                if (title == "ESL Capable")
                    messageParts.AddRange(new[]
                    {
                        $"❓ There are {plugins.Count} plugins that can be given the ESL flag. This can be done with\n",
                        "  the SimpleESLify script to avoid reaching the plugin limit (254 esm/esp).\n",
                        $"  SimpleESLify: {ResourceLinks["simple_eslify"]}\n  -----\n"
                    });

                // Add any matching warnings from settings
                foreach (var (warningName, warningText) in wryeWarnings)
                    if (title.Contains(warningName))
                        messageParts.Add(warningText);

                // List plugins (except for special sections)
                if (title is not "ESL Capable" and not "Active Plugins:")
                    messageParts.AddRange(plugins.Select(plugin => $"    > {plugin}\n"));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error parsing Wrye Bash report: {ReportPath}", reportPath);
            messageParts.Add("❌ ERROR: Failed to parse Wrye Bash report\n-----\n");
        }

        return messageParts;
    }

    /// <summary>
    /// Extracts plugin file names from a specified section of the HTML document.
    /// </summary>
    /// <param name="section">The header element representing the section from which plugins are to be extracted.</param>
    /// <returns>A list of plugin file names found within the given section.</returns>
    private static List<string> ExtractPluginsFromSection(IElement section)
    {
        var plugins = new List<string>();
        var nextSibling = section.NextElementSibling;

        while (nextSibling != null)
        {
            // Stop if we've moved to a different section
            if (nextSibling.TagName.Equals("H3", StringComparison.OrdinalIgnoreCase)) break;

            // Process the plugin entry
            if (nextSibling.TagName.Equals("P", StringComparison.OrdinalIgnoreCase))
            {
                var text = nextSibling.TextContent?.Trim().Replace("•\u00A0 ", "") ?? "";
                if (text.Contains(".esp") || text.Contains(".esl") || text.Contains(".esm")) plugins.Add(text);
            }

            nextSibling = nextSibling.NextElementSibling;
        }

        return plugins;
    }

    /// <summary>
    /// Formats a section header with adjustable padding to center-align the title.
    /// </summary>
    /// <param name="title">The title string to be formatted as a section header.</param>
    /// <returns>The formatted section header with or without applied padding.</returns>
    private static string FormatSectionHeader(string title)
    {
        if (title.Length < 32)
        {
            var diff = 32 - title.Length;
            var left = diff / 2;
            var right = diff - left;
            return $"\n   {new string('=', left)} {title} {new string('=', right)}\n";
        }

        return title;
    }

    /// <summary>
    /// Reads a file with automatic encoding detection.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <returns>The file content as a string with proper encoding.</returns>
    private async Task<string> ReadFileWithEncodingAsync(string filePath)
    {
        var fileBytes = await _fileSystem.File.ReadAllBytesAsync(filePath);

        // Detect encoding
        var detector = new CharsetDetector();
        detector.Feed(fileBytes, 0, fileBytes.Length);
        detector.DataEnd();

        var encoding = Encoding.UTF8; // Default fallback
        if (detector.Charset != null)
            try
            {
                encoding = Encoding.GetEncoding(detector.Charset);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get encoding {Charset}, using UTF-8", detector.Charset);
            }

        return encoding.GetString(fileBytes);
    }
}
