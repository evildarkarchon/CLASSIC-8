using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Classic.Core.Models;
using Classic.ScanLog.Configuration;
using Classic.ScanLog.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Examples
{
    /// <summary>
    /// Example showing how to configure and use the unified report generator
    /// </summary>
    public class ReportGenerationExample
    {
        public static async Task Main(string[] args)
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Get the unified report generator
            var reportGenerator = serviceProvider.GetRequiredService<UnifiedReportGenerator>();
            
            // Example 1: Generate a standard report (non-FCX mode)
            await GenerateStandardReport(reportGenerator);
            
            // Example 2: Generate an advanced report (FCX mode)
            await GenerateAdvancedReport(reportGenerator);
            
            // Example 3: Generate report based on user settings
            await GenerateReportFromSettings(reportGenerator);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Register report templates
            services.AddSingleton<IReportTemplate, MarkdownReportTemplate>();
            
            // Register section generators
            services.AddTransient<ISectionGenerator, HeaderSectionGenerator>();
            services.AddTransient<ISectionGenerator, ErrorSectionGenerator>();
            services.AddTransient<ISectionGenerator, SettingsSectionGenerator>();
            services.AddTransient<ISectionGenerator, SuspectSectionGenerator>();
            services.AddTransient<ISectionGenerator, PluginSectionGenerator>();
            services.AddTransient<ISectionGenerator, FormIdSectionGenerator>();
            services.AddTransient<ISectionGenerator, NamedRecordSectionGenerator>();
            services.AddTransient<ISectionGenerator, FooterSectionGenerator>();
            
            // FCX-specific generators
            services.AddTransient<ISectionGenerator, FCXNoticeSectionGenerator>();
            services.AddTransient<ISectionGenerator, MainFilesCheckGenerator>();
            services.AddTransient<ISectionGenerator, GameFilesCheckGenerator>();
            services.AddTransient<ISectionGenerator, PerformanceMetricsGenerator>();
            services.AddTransient<ISectionGenerator, GameHintsGenerator>();
            
            // Register strategies
            services.AddSingleton<IReportStrategy, StandardReportStrategy>();
            services.AddSingleton<IReportStrategy, AdvancedReportStrategy>();
            
            // Register strategy factory
            services.AddSingleton<IReportStrategyFactory, ReportStrategyFactory>();
            
            // Register the unified report generator
            services.AddScoped<UnifiedReportGenerator>();
            
            // Register configuration
            services.Configure<ReportGenerationOptions>(options =>
            {
                options.DefaultOutputPath = "Reports";
                options.DefaultTemplate = "Markdown";
                options.EnablePerformanceMetrics = true;
                options.MaxGameHints = 3;
            });
        }

        private static async Task GenerateStandardReport(UnifiedReportGenerator generator)
        {
            Console.WriteLine("Generating Standard Report...");
            
            // Create sample analysis result
            var analysisResult = CreateSampleAnalysisResult();
            
            // Configure options for standard report
            var options = new ReportOptions
            {
                FCXMode = false,
                OutputPath = "standard_report.md",
                IncludeDebugInfo = false
            };
            
            // Generate report
            var report = await generator.GenerateReportAsync(analysisResult, options);
            
            Console.WriteLine($"Standard report generated: {report.Length} characters");
            Console.WriteLine("Report preview:");
            Console.WriteLine(report.Substring(0, Math.Min(500, report.Length)) + "...");
        }

        private static async Task GenerateAdvancedReport(UnifiedReportGenerator generator)
        {
            Console.WriteLine("\nGenerating Advanced Report (FCX Mode)...");
            
            // Create sample analysis result with FCX data
            var analysisResult = CreateSampleAnalysisResultWithFCX();
            
            // Configure options for advanced report
            var options = new ReportOptions
            {
                FCXMode = true,
                OutputPath = "advanced_report.md",
                IncludeDebugInfo = true,
                IncludePerformanceMetrics = true,
                IncludeGameHints = true
            };
            
            // Generate report
            var report = await generator.GenerateReportAsync(analysisResult, options);
            
            Console.WriteLine($"Advanced report generated: {report.Length} characters");
            Console.WriteLine("FCX sections included: Main Files Check, Game Files Check, Performance Metrics");
        }

        private static async Task GenerateReportFromSettings(UnifiedReportGenerator generator)
        {
            Console.WriteLine("\nGenerating Report Based on User Settings...");
            
            // Load user settings (in real app, this would come from YAML/JSON)
            var userSettings = new ClassicSettings
            {
                FCXMode = true,
                ShowFormIDValues = true,
                MoveUnsolvedLogs = false,
                ReportFormat = "Markdown",
                EnableGameHints = true
            };
            
            // Create analysis result
            var analysisResult = CreateSampleAnalysisResultWithFCX();
            
            // Configure options from user settings
            var options = new ReportOptions
            {
                FCXMode = userSettings.FCXMode,
                OutputPath = $"CLASSIC-Report-{DateTime.Now:yyyyMMdd-HHmmss}.md",
                IncludeFormIdValues = userSettings.ShowFormIDValues,
                IncludeGameHints = userSettings.EnableGameHints,
                Template = userSettings.ReportFormat
            };
            
            // Generate report
            var report = await generator.GenerateReportAsync(analysisResult, options);
            
            Console.WriteLine($"User-configured report generated with FCX Mode: {userSettings.FCXMode}");
        }

        private static CrashLogAnalysisResult CreateSampleAnalysisResult()
        {
            return new CrashLogAnalysisResult
            {
                CrashLog = new CrashLog
                {
                    FileName = "crash-2024-01-15-120530.txt",
                    MainError = "EXCEPTION_ACCESS_VIOLATION",
                    CrashGenVersion = "1.27.0",
                    GameVersion = "1.10.984"
                },
                ClassicVersion = "CLASSIC-8 v8.0.0",
                CrashGenName = "Buffout 4 NG",
                IsOutdated = false,
                CrashSuspects = new List<Suspect>
                {
                    new Suspect
                    {
                        Name = "BA2Limit",
                        Description = "Too many BA2 archives loaded",
                        SeverityScore = 5,
                        Evidence = "BA2 count: 512",
                        Recommendation = "Reduce number of mods or merge archives"
                    }
                },
                SettingsValidation = new SettingsValidation
                {
                    Issues = new List<SettingsIssue>
                    {
                        new SettingsIssue
                        {
                            SettingName = "MemoryManager",
                            CurrentValue = "false",
                            ExpectedValue = "true",
                            Description = "Memory manager should be enabled",
                            SeverityScore = 4
                        }
                    }
                },
                ProblematicPlugins = new List<Plugin>
                {
                    new Plugin
                    {
                        LoadOrder = 0xFF,
                        Name = "TooManyMods.esp",
                        Status = PluginStatus.Active,
                        HasIssues = true
                    }
                }
            };
        }

        private static CrashLogAnalysisResult CreateSampleAnalysisResultWithFCX()
        {
            var result = CreateSampleAnalysisResult();
            
            // Add FCX-specific data
            result.FCXMode = true;
            result.MainFilesValidation = new MainFilesValidation
            {
                FileChecks = new List<FileCheck>
                {
                    new FileCheck
                    {
                        FileName = "Fallout4.exe",
                        IsValid = true,
                        ErrorMessage = null
                    },
                    new FileCheck
                    {
                        FileName = "Data\\Fallout4.esm",
                        IsValid = false,
                        ErrorMessage = "File modified - integrity check failed"
                    }
                }
            };
            
            result.GameFilesValidation = new GameFilesValidation
            {
                MissingFiles = new List<string> { "Data\\Textures\\actors.ba2" },
                CorruptedFiles = new List<CorruptedFile>
                {
                    new CorruptedFile
                    {
                        FileName = "Data\\Meshes\\architecture.ba2",
                        Issue = "CRC mismatch"
                    }
                }
            };
            
            result.PerformanceMetrics = new PerformanceMetrics
            {
                ProcessingTime = TimeSpan.FromSeconds(2.34),
                FilesAnalyzed = 1523,
                PatternsMatched = 47,
                MemoryUsageMB = 156.7
            };
            
            return result;
        }
    }

    /// <summary>
    /// Report generation options
    /// </summary>
    public class ReportOptions
    {
        public bool FCXMode { get; set; }
        public string OutputPath { get; set; } = "report.md";
        public string Template { get; set; } = "Markdown";
        public bool IncludeDebugInfo { get; set; }
        public bool IncludeFormIdValues { get; set; } = true;
        public bool IncludePerformanceMetrics { get; set; } = true;
        public bool IncludeGameHints { get; set; } = true;
    }

    /// <summary>
    /// User settings from YAML/JSON configuration
    /// </summary>
    public class ClassicSettings
    {
        public bool FCXMode { get; set; }
        public bool ShowFormIDValues { get; set; }
        public bool MoveUnsolvedLogs { get; set; }
        public string ReportFormat { get; set; } = "Markdown";
        public bool EnableGameHints { get; set; }
        public int MaxConcurrentScans { get; set; } = 4;
    }

    /// <summary>
    /// Report generation configuration
    /// </summary>
    public class ReportGenerationOptions
    {
        public string DefaultOutputPath { get; set; } = "Reports";
        public string DefaultTemplate { get; set; } = "Markdown";
        public bool EnablePerformanceMetrics { get; set; } = true;
        public int MaxGameHints { get; set; } = 3;
        public Dictionary<string, List<string>> SectionsByMode { get; set; } = new()
        {
            ["Standard"] = new List<string>
            {
                "Header", "MainError", "CrashSuspects", "Settings",
                "PluginSuspects", "FormIdSuspects", "NamedRecords", "Footer"
            },
            ["FCX"] = new List<string>
            {
                "Header", "FCXNotice", "MainError", "CrashSuspects", "Settings",
                "MainFilesCheck", "GameFilesCheck", "ModConflicts",
                "PluginSuspects", "FormIdSuspects", "NamedRecords",
                "Performance", "GameHints", "Footer"
            }
        };
    }

    /// <summary>
    /// Simple implementation of the strategy factory
    /// </summary>
    public class ReportStrategyFactory : IReportStrategyFactory
    {
        private readonly IEnumerable<IReportStrategy> _strategies;

        public ReportStrategyFactory(IEnumerable<IReportStrategy> strategies)
        {
            _strategies = strategies;
        }

        public IReportStrategy GetStrategy(bool fcxMode)
        {
            return _strategies.FirstOrDefault(s => s.RequiresFCXMode == fcxMode)
                   ?? throw new InvalidOperationException($"No strategy found for FCX mode: {fcxMode}");
        }
    }
}