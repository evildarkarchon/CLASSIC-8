using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Services
{
    /// <summary>
    /// Service for rendering report templates with data models
    /// </summary>
    public interface ITemplateRenderingService
    {
        Task<string> RenderTemplateAsync(string templatePath, object model);
        string RenderTemplate(string templateContent, object model);
    }

    /// <summary>
    /// Simple template rendering implementation
    /// </summary>
    public class SimpleTemplateRenderingService : ITemplateRenderingService
    {
        private readonly ILogger<SimpleTemplateRenderingService> _logger;
        
        public SimpleTemplateRenderingService(ILogger<SimpleTemplateRenderingService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RenderTemplateAsync(string templatePath, object model)
        {
            var templateContent = await File.ReadAllTextAsync(templatePath);
            return RenderTemplate(templateContent, model);
        }

        public string RenderTemplate(string templateContent, object model)
        {
            // Simple placeholder replacement
            var result = templateContent;
            var properties = model.GetType().GetProperties();
            
            foreach (var prop in properties)
            {
                var value = prop.GetValue(model);
                var placeholder = $"{{{{{prop.Name}}}}}";
                
                if (value != null)
                {
                    result = result.Replace(placeholder, value.ToString());
                }
            }
            
            // Handle conditionals
            result = ProcessConditionals(result, model);
            
            // Handle loops
            result = ProcessLoops(result, model);
            
            return result;
        }

        private string ProcessConditionals(string template, object model)
        {
            // Simple if/else processing
            var ifPattern = @"{{#if\s+(\w+)}}(.*?){{/if}}";
            var ifElsePattern = @"{{#if\s+(\w+)}}(.*?){{else}}(.*?){{/if}}";
            
            // Process if/else blocks
            template = Regex.Replace(template, ifElsePattern, match =>
            {
                var propertyName = match.Groups[1].Value;
                var ifContent = match.Groups[2].Value;
                var elseContent = match.Groups[3].Value;
                
                var value = GetPropertyValue(model, propertyName);
                var condition = EvaluateCondition(value);
                
                return condition ? ifContent : elseContent;
            }, RegexOptions.Singleline);
            
            // Process simple if blocks
            template = Regex.Replace(template, ifPattern, match =>
            {
                var propertyName = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                
                var value = GetPropertyValue(model, propertyName);
                var condition = EvaluateCondition(value);
                
                return condition ? content : string.Empty;
            }, RegexOptions.Singleline);
            
            // Process unless blocks
            var unlessPattern = @"{{#unless\s+(\w+)}}(.*?){{/unless}}";
            template = Regex.Replace(template, unlessPattern, match =>
            {
                var propertyName = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                
                var value = GetPropertyValue(model, propertyName);
                var condition = !EvaluateCondition(value);
                
                return condition ? content : string.Empty;
            }, RegexOptions.Singleline);
            
            return template;
        }

        private string ProcessLoops(string template, object model)
        {
            var eachPattern = @"{{#each\s+(\w+)}}(.*?){{/each}}";
            
            return Regex.Replace(template, eachPattern, match =>
            {
                var propertyName = match.Groups[1].Value;
                var itemTemplate = match.Groups[2].Value;
                
                var value = GetPropertyValue(model, propertyName);
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var result = new StringBuilder();
                    var index = 0;
                    
                    foreach (var item in enumerable)
                    {
                        var itemResult = itemTemplate;
                        
                        // Replace {{this}}
                        itemResult = itemResult.Replace("{{this}}", item?.ToString() ?? string.Empty);
                        
                        // Replace {{@index}}
                        itemResult = itemResult.Replace("{{@index}}", index.ToString());
                        itemResult = itemResult.Replace("{{@index+1}}", (index + 1).ToString());
                        
                        // Replace item properties
                        if (item != null)
                        {
                            var itemProperties = item.GetType().GetProperties();
                            foreach (var prop in itemProperties)
                            {
                                var propValue = prop.GetValue(item);
                                itemResult = itemResult.Replace($"{{{{{prop.Name}}}}}", propValue?.ToString() ?? string.Empty);
                            }
                        }
                        
                        result.Append(itemResult);
                        index++;
                    }
                    
                    return result.ToString();
                }
                
                return string.Empty;
            }, RegexOptions.Singleline);
        }

        private object GetPropertyValue(object model, string propertyName)
        {
            var property = model.GetType().GetProperty(propertyName);
            return property?.GetValue(model);
        }

        private bool EvaluateCondition(object value)
        {
            if (value == null) return false;
            if (value is bool boolValue) return boolValue;
            if (value is int intValue) return intValue > 0;
            if (value is string stringValue) return !string.IsNullOrEmpty(stringValue);
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
                return enumerable.Cast<object>().Any();
            
            return true;
        }
    }

    /// <summary>
    /// Report template service that manages templates and rendering
    /// </summary>
    public class ReportTemplateService
    {
        private readonly ITemplateRenderingService _renderingService;
        private readonly ILogger<ReportTemplateService> _logger;
        private readonly Dictionary<string, string> _templateCache = new();

        public ReportTemplateService(
            ITemplateRenderingService renderingService,
            ILogger<ReportTemplateService> logger)
        {
            _renderingService = renderingService;
            _logger = logger;
            
            // Load built-in templates
            LoadBuiltInTemplates();
        }

        private void LoadBuiltInTemplates()
        {
            // In a real implementation, these would be loaded from files
            _templateCache["Standard"] = GetStandardTemplate();
            _templateCache["Enhanced"] = GetEnhancedTemplate();
            _templateCache["Advanced"] = GetAdvancedTemplate();
        }

        public async Task<string> GenerateStandardReportAsync(StandardReportModel model)
        {
            try
            {
                var template = _templateCache["Standard"];
                return await Task.Run(() => _renderingService.RenderTemplate(template, model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate standard report");
                return GenerateFallbackReport(model);
            }
        }

        public async Task<string> GenerateEnhancedReportAsync(EnhancedReportModel model)
        {
            try
            {
                var template = _templateCache["Enhanced"];
                return await Task.Run(() => _renderingService.RenderTemplate(template, model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate enhanced report");
                return GenerateFallbackReport(model);
            }
        }

        public async Task<string> GenerateAdvancedReportAsync(AdvancedReportModel model)
        {
            try
            {
                var template = _templateCache["Advanced"];
                return await Task.Run(() => _renderingService.RenderTemplate(template, model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate advanced report");
                return GenerateFallbackReport(model);
            }
        }

        private string GenerateFallbackReport(ReportDataModel model)
        {
            return $@"# Crash Log Analysis Report - Error

**File:** {model.FileName}
**Date:** {model.GeneratedDate}

An error occurred while generating the full report. 
Please check the logs for more information.

Basic Information:
- Main Error: {model.MainError}
- Crash Generator: {model.CrashGenName} v{model.CrashGenVersion}
- Critical Issues: {model.HasCriticalIssues}
";
        }

        // Template content methods (these would normally load from files)
        private string GetStandardTemplate()
        {
            // Return the standard template content
            // This is a simplified version - the full template would be loaded from a file
            return @"# Crash Log Analysis Report

**File:** {{FileName}}
**Generated by:** {{ClassicVersion}}
**Date:** {{GeneratedDate}}

> ⚠️ **Note:** Please read everything carefully and beware of false positives.

---

## ERROR ANALYSIS

Main Error: {{MainError}}
Detected {{CrashGenName}} Version: {{CrashGenVersion}}

{{#if IsOutdated}}
* ⚠️ WARNING: Your {{CrashGenName}} is outdated! Please update to the latest version. *
{{else}}
* You have the latest version of {{CrashGenName}}! *
{{/if}}

---

## CHECKING IF NECESSARY FILES/SETTINGS ARE CORRECT...

{{#each SettingsIssues}}
### {{SeverityIcon}} {{SettingName}}
- **Current Value:** {{CurrentValue}}
- **Expected Value:** {{ExpectedValue}}
- **Description:** {{Description}}
{{#if Recommendation}}
- **Recommendation:** {{Recommendation}}
{{/if}}

{{/each}}

{{#unless SettingsIssues}}
✅ All settings appear to be correctly configured.
{{/unless}}

---

[Rest of template content...]
";
        }

        private string GetEnhancedTemplate()
        {
            // Return the enhanced template content (advanced formatting without FCX)
            // This is a simplified version - the full template would be loaded from a file
            return @"╔════════════════════════════════════════════════════════════════════════════════╗
║  CLASSIC-8 Crash Log Analysis Report                                             ║
║  Comprehensive Analysis and Recommendations                                      ║
╚════════════════════════════════════════════════════════════════════════════════╝

📂 **File:** {{FileName}}
🕐 **Generated:** {{GeneratedDate}}
⚡ **Processing Time:** {{ProcessingTimeSeconds}}s
🎮 **Game:** {{GameName}}
🔧 **CLASSIC Version:** {{ClassicVersion}}

* NOTICE: FCX MODE IS DISABLED. YOU CAN ENABLE IT TO DETECT PROBLEMS IN YOUR MOD & GAME FILES *

[Rest of enhanced template content...]
";
        }

        private string GetAdvancedTemplate()
        {
            // Return the advanced template content (with FCX mode)
            // This is a simplified version - the full template would be loaded from a file
            return @"╔════════════════════════════════════════════════════════════════════════════════╗
║  CLASSIC-8 Crash Log Analysis Report                                             ║
║  Comprehensive Analysis and Recommendations                                      ║
╚════════════════════════════════════════════════════════════════════════════════╝

📂 **File:** {{FileName}}
🕐 **Generated:** {{GeneratedDate}}
⚡ **Processing Time:** {{ProcessingTimeSeconds}}s
🎮 **Game:** {{GameName}}
🔧 **CLASSIC Version:** {{ClassicVersion}}

* NOTICE: FCX MODE IS ENABLED. CLASSIC MUST BE RUN BY THE ORIGINAL USER FOR CORRECT DETECTION *

[Rest of template content with FCX sections...]
";
        }
    }

    /// <summary>
    /// Example usage of the template service
    /// </summary>
    public class ReportGenerationExamples
    {
        public static async Task GenerateExampleReports()
        {
            // Set up services
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<ReportTemplateService>();
            var renderLogger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<SimpleTemplateRenderingService>();
            
            var renderingService = new SimpleTemplateRenderingService(renderLogger);
            var templateService = new ReportTemplateService(renderingService, logger);
            
            // Example 1: Generate Standard Report
            var standardModel = new StandardReportModel
            {
                FileName = "crash-2024-01-15-120530.txt",
                ClassicVersion = "CLASSIC-8 v8.0.0",
                GeneratedDate = DateTime.Now,
                MainError = "EXCEPTION_ACCESS_VIOLATION",
                CrashGenName = "Buffout 4 NG",
                CrashGenVersion = "1.27.0",
                GameVersion = "1.10.984",
                IsOutdated = false,
                
                SettingsIssues = new List<SettingsIssueModel>
                {
                    new SettingsIssueModel
                    {
                        SettingName = "MemoryManager",
                        CurrentValue = "false",
                        ExpectedValue = "true",
                        Description = "Memory manager should be enabled for stability",
                        Recommendation = "Enable in Buffout4.toml",
                        Severity = 4
                    }
                },
                
                CrashSuspects = new List<CrashSuspectModel>
                {
                    new CrashSuspectModel
                    {
                        Name = "BA2Limit",
                        Description = "Too many BA2 archives loaded",
                        Evidence = "BA2 count: 512",
                        Recommendation = "Reduce number of mods or merge archives",
                        Severity = 5
                    }
                }
            };
            
            var standardReport = await templateService.GenerateStandardReportAsync(standardModel);
            Console.WriteLine("Standard Report Generated:");
            Console.WriteLine(standardReport.Substring(0, Math.Min(500, standardReport.Length)) + "...");
            
            // Example 2: Generate Enhanced Report (Non-FCX with Game Hints)
            var enhancedModel = new EnhancedReportModel
            {
                // Copy standard properties
                FileName = standardModel.FileName,
                ClassicVersion = standardModel.ClassicVersion,
                GeneratedDate = standardModel.GeneratedDate,
                MainError = standardModel.MainError,
                CrashGenName = standardModel.CrashGenName,
                CrashGenVersion = standardModel.CrashGenVersion,
                GameVersion = standardModel.GameVersion,
                IsOutdated = standardModel.IsOutdated,
                SettingsIssues = standardModel.SettingsIssues,
                CrashSuspects = standardModel.CrashSuspects,
                
                // Add enhanced properties
                GameName = "Fallout 4",
                ProcessingTime = TimeSpan.FromSeconds(2.34),
                CrashAddress = "0x7FF6A1234567",
                ProbableCause = "Memory access violation in texture loading",
                
                CompatibilityScore = 78,
                StabilityScore = 65,
                RiskLevel = "Medium",
                OverallRecommendation = "Fix critical issues and verify load order",
                
                // Game hints are available without FCX (same as Python)
                GameHints = new List<string>
                {
                    "Consider using BethINI to optimize your INI settings for better stability",
                    "Enable crash logs in all modding tools to catch issues early",
                    "Keep backup saves and use the console command 'player.kill' if stuck"
                }
            };
            
            var enhancedReport = await templateService.GenerateEnhancedReportAsync(enhancedModel);
            Console.WriteLine("\nEnhanced Report Generated:");
            Console.WriteLine(enhancedReport.Substring(0, Math.Min(500, enhancedReport.Length)) + "...");
            
            // Example 3: Generate Advanced Report (FCX Mode)
            var advancedModel = new AdvancedReportModel
            {
                // Copy enhanced properties
                FileName = enhancedModel.FileName,
                ClassicVersion = enhancedModel.ClassicVersion,
                GeneratedDate = enhancedModel.GeneratedDate,
                MainError = enhancedModel.MainError,
                CrashGenName = enhancedModel.CrashGenName,
                CrashGenVersion = enhancedModel.CrashGenVersion,
                GameVersion = enhancedModel.GameVersion,
                IsOutdated = enhancedModel.IsOutdated,
                SettingsIssues = enhancedModel.SettingsIssues,
                CrashSuspects = enhancedModel.CrashSuspects,
                GameName = enhancedModel.GameName,
                ProcessingTime = enhancedModel.ProcessingTime,
                CompatibilityScore = enhancedModel.CompatibilityScore,
                StabilityScore = enhancedModel.StabilityScore,
                RiskLevel = enhancedModel.RiskLevel,
                GameHints = enhancedModel.GameHints,
                
                // Add FCX-specific properties
                MainFilesChecks = new List<MainFileCheckModel>
                {
                    new MainFileCheckModel
                    {
                        FileName = "Fallout4.exe",
                        IsValid = true,
                        FileSize = "92.5 MB"
                    },
                    new MainFileCheckModel
                    {
                        FileName = "Data\\Fallout4.esm",
                        IsValid = false,
                        ErrorMessage = "File modified - integrity check failed",
                        RecommendedAction = "Verify game files through Steam"
                    }
                },
                
                MissingGameFiles = new List<MissingGameFileModel>
                {
                    new MissingGameFileModel
                    {
                        FileName = "Data\\Textures\\actors.ba2",
                        Impact = "Missing actor textures may cause visual glitches",
                        Solution = "Verify game files or reinstall"
                    }
                },
                
                WorkerThreadsUsed = 4,
                TotalFilesProcessed = 1523,
                PatternMatchTime = 0.45,
                FileIOTime = 1.23,
                AnalysisTime = 0.52,
                ReportGenTime = 0.14,
                
                OverallRecommendation = "Fix critical issues and verify game files integrity"
            };
            
            var advancedReport = await templateService.GenerateAdvancedReportAsync(advancedModel);
            Console.WriteLine("\nAdvanced Report Generated (FCX Mode):");
            Console.WriteLine(advancedReport.Substring(0, Math.Min(500, advancedReport.Length)) + "...");
        }
    }
}