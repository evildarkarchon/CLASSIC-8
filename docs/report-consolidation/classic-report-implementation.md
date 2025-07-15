# CLASSIC C# Port - Unified Report Writing Implementation Strategy

## Executive Summary

This report outlines a strategy for implementing a unified report writing solution in the C# port of CLASSIC that handles three distinct report formats while maintaining feature parity with the Python version. The solution leverages the existing template-based architecture in C# while introducing a strategy pattern to handle format-specific differences.

### Key Innovation: Three-Tier Template System
- **Standard Template**: Basic format for quick analysis
- **Enhanced Template**: Advanced formatting with game hints (no FCX required)
- **Advanced Template**: Full FCX mode with file system checks

> **Important**: Game hints are available in both Enhanced and Advanced templates, matching Python's behavior where hints are not tied to FCX mode.

## Current State Analysis

### Python Implementation

The Python version of CLASSIC generates reports with consistent core features, with additional sections when FCX mode is enabled:

1. **Core Report Features (Always Present)**
   - Crash log analysis
   - Crash suspects detection
   - Plugin analysis
   - FormID analysis
   - Settings validation
   - Named records scanning
   - Game-specific hints and tips

2. **FCX Mode Additional Features**
   - Main files validation
   - Game files integrity checking
   - Extended file I/O metrics
   - Worker thread performance data

### Existing C# Infrastructure

The C# port already has a solid foundation:

```csharp
// Core interfaces
IReportTemplate - Defines report formatting contract
IReportGenerator - Handles report generation logic

// Implementations
MarkdownReportTemplate - Markdown formatting
ReportGenerator - Basic report generation
AdvancedReportGenerator - Comprehensive report features
```

### Improvement Over Python

While Python CLASSIC has a single report format that adds sections based on FCX mode, the C# implementation improves user experience by offering three distinct templates:

1. **Standard** - Minimal format for basic needs
2. **Enhanced** - Professional format without FCX overhead  
3. **Advanced** - Full features with FCX file checks

This provides better performance and cleaner output while maintaining complete feature parity.

## Proposed Unified Architecture

### 1. Core Strategy Pattern

Implement a strategy pattern to handle three report formats while sharing common functionality:

```csharp
public interface IReportStrategy
{
    string Name { get; }
    ReportFormat Format { get; }
    bool RequiresFCXMode { get; }
    Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken);
}

public class StandardReportStrategy : IReportStrategy
{
    public string Name => "Standard Report";
    public ReportFormat Format => ReportFormat.Standard;
    public bool RequiresFCXMode => false;
    
    public async Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        // Generate basic sections with simple formatting
    }
}

public class EnhancedReportStrategy : IReportStrategy
{
    public string Name => "Enhanced Report";
    public ReportFormat Format => ReportFormat.Enhanced;
    public bool RequiresFCXMode => false;
    
    public async Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        // Generate all sections with advanced formatting
        // Includes game hints but no FCX file checks
    }
}

public class AdvancedReportStrategy : IReportStrategy
{
    public string Name => "Advanced Report (FCX Mode)";
    public ReportFormat Format => ReportFormat.Advanced;
    public bool RequiresFCXMode => true;
    
    public async Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        // Generate all sections including FCX-specific
    }
}
```

### 2. Unified Report Generator

Create a unified generator that selects the appropriate strategy:

```csharp
public class UnifiedReportGenerator : IReportGenerator
{
    private readonly IReportTemplate _template;
    private readonly IReportStrategyFactory _strategyFactory;
    private readonly ILogger<UnifiedReportGenerator> _logger;
    
    public async Task<string> GenerateReportAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken)
    {
        // Select strategy based on options
        var strategy = SelectStrategy(options, analysisResult);
        
        _logger.LogInformation("Selected {Strategy} for report generation", 
            strategy.Name);
        
        // Generate sections using strategy
        var sections = await strategy.GenerateSectionsAsync(
            analysisResult, cancellationToken);
        
        // Apply template formatting
        return await ApplyTemplateAsync(sections, strategy.Format);
    }
    
    private IReportStrategy SelectStrategy(
        ReportOptions options, 
        CrashLogAnalysisResult analysisResult)
    {
        // Decision tree for strategy selection
        if (options.FCXMode)
        {
            return _strategyFactory.GetStrategy(ReportFormat.Advanced);
        }
        
        // Check if enhanced format is requested or beneficial
        if (options.UseEnhancedFormatting || 
            ShouldUseEnhancedFormat(analysisResult))
        {
            return _strategyFactory.GetStrategy(ReportFormat.Enhanced);
        }
        
        return _strategyFactory.GetStrategy(ReportFormat.Standard);
    }
    
    private bool ShouldUseEnhancedFormat(CrashLogAnalysisResult result)
    {
        // Use enhanced format for complex crashes
        return result.CrashSuspects.Count >= 5 ||
               result.ModCompatibilityIssues.Count >= 3 ||
               result.SettingsValidation.Issues.Any(i => i.SeverityScore >= 4);
    }
}
```

### 3. Common Report Sections

All report types share these core sections:

```csharp
public class ReportSections
{
    // Common sections for all report formats
    public HeaderSection Header { get; set; }
    public ErrorSection MainError { get; set; }
    public List<SuspectSection> CrashSuspects { get; set; }
    public SettingsValidationSection Settings { get; set; }
    public List<PluginSection> PluginSuspects { get; set; }
    public List<FormIdSection> FormIdSuspects { get; set; }
    public List<NamedRecordSection> NamedRecords { get; set; }
    public FooterSection Footer { get; set; }
    
    // Enhanced/Advanced formatting sections
    public ExecutiveSummarySection? ExecutiveSummary { get; set; }
    public PerformanceMetricsSection? Performance { get; set; }
    public GameHintsSection? GameHints { get; set; }
    
    // FCX-specific sections (Advanced only)
    public FCXNoticeSection? FCXNotice { get; set; }
    public MainFilesCheckSection? MainFilesCheck { get; set; }
    public GameFilesCheckSection? GameFilesCheck { get; set; }
    public ExtendedPerformanceSection? ExtendedPerformance { get; set; }
}
```

### 4. Section Generators

Implement modular section generators that can be composed:

```csharp
public interface ISectionGenerator<TSection>
{
    Task<TSection> GenerateAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken);
}

public class HeaderSectionGenerator : ISectionGenerator<HeaderSection>
{
    public async Task<HeaderSection> GenerateAsync(
        CrashLogAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        return new HeaderSection
        {
            FileName = analysisResult.CrashLog.FileName,
            Version = analysisResult.ClassicVersion,
            GeneratedDate = DateTime.Now,
            FCXModeEnabled = analysisResult.FCXMode
        };
    }
}
```

### 5. FCX Mode Integration

Handle FCX-specific features elegantly:

```csharp
public class FCXModeHandler
{
    private readonly IMainFilesValidator _mainFilesValidator;
    private readonly IGameFilesValidator _gameFilesValidator;
    
    public async Task<FCXCheckResults> PerformFCXChecksAsync(
        GameConfiguration gameConfig,
        CancellationToken cancellationToken)
    {
        var results = new FCXCheckResults();
        
        // Perform main files validation
        results.MainFilesResult = await _mainFilesValidator
            .ValidateAsync(gameConfig.MainFiles, cancellationToken);
        
        // Perform game files validation
        results.GameFilesResult = await _gameFilesValidator
            .ValidateAsync(gameConfig.GamePath, cancellationToken);
        
        return results;
    }
}
```

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)
1. Implement core interfaces and base classes
2. Create section generator infrastructure
3. Set up three-tier strategy pattern framework
4. Implement template selection logic
5. Port common validation logic from Python

### Phase 2: Standard Report (Week 3)
Note: This phase might have infrastructure already in place from the initial C# port, modify it to comply as needed.
1. Implement basic section generators
2. Create Standard template with simple Markdown
3. Port crash suspect detection logic
4. Basic plugin and FormID analysis
5. Settings validation

### Phase 3: Enhanced Report (Week 4-5)
1. Implement Enhanced template with rich formatting
2. Add priority grouping for suspects
3. Add executive summary generation
4. Implement game hints loader
5. Add performance metrics (non-FCX)
6. Create compatibility scoring system

### Phase 4: Advanced Report / FCX Mode (Week 6-7)
1. Implement FCX mode handler
2. Add main files validation
3. Add game files validation
4. Integrate extended performance metrics
5. Add file I/O and worker thread tracking

### Phase 5: Polish & Testing (Week 8)
1. Add comprehensive unit tests for all templates
2. Performance optimization
3. Add configuration system for template selection
4. Documentation and migration guide

## Key Design Decisions

### 1. Three-Tier Template System
Provides flexibility while maintaining simplicity:

| Template | Use Case | Key Features | File Size |
|----------|----------|--------------|-----------|
| Standard | Quick analysis, API calls | Basic info, simple format | ~3-5 KB |
| Enhanced | Detailed analysis without FCX | Rich format, game hints, summaries | ~8-12 KB |
| Advanced | Full system analysis | Everything + FCX file checks | ~15-25 KB |

### 2. Template Abstraction
Maintain separation between content generation and formatting to support multiple output formats.

### 3. Async Throughout
Use async/await patterns consistently for I/O operations and potential parallel processing.

### 4. Composition Over Inheritance
Use composition of section generators rather than complex inheritance hierarchies.

### 5. Configuration-Driven
Make the system configurable through YAML/JSON files, matching Python's approach.

### 6. Smart Defaults
Automatically select Enhanced template for complex crashes even without FCX mode.

## Configuration Example

```yaml
ReportConfiguration:
  # Template selection mode
  TemplateSelection:
    Mode: "Auto"  # Auto, Manual, ForceStandard, ForceEnhanced
    EnhancedThreshold:
      MinSuspects: 5
      MinConflicts: 3
      MinSeverity: 4
  
  StandardMode:
    Sections:
      - Header
      - MainError
      - CrashSuspects
      - Settings
      - PluginSuspects
      - FormIdSuspects
      - NamedRecords
      - Footer
    Formatting:
      UseMarkdown: true
      UseEmojis: false
      UseUnicodeBoxes: false
  
  EnhancedMode:
    Sections:
      - Header
      - MainError
      - ExecutiveSummary
      - CrashSuspects  # With priority grouping
      - Settings
      - ModConflicts   # With categories
      - PluginSuspects # With detailed status
      - FormIdSuspects # With analysis summary
      - NamedRecords   # Grouped by type
      - Performance    # Basic metrics
      - GameHints      # Full hints
      - Footer
    Formatting:
      UseMarkdown: true
      UseEmojis: true
      UseUnicodeBoxes: true
      UseColorCoding: true
    
  FCXMode:
    Sections:
      - Header
      - FCXNotice
      - MainError
      - ExecutiveSummary
      - CrashSuspects
      - Settings
      - MainFilesCheck    # FCX only
      - GameFilesCheck    # FCX only
      - ModConflicts
      - PluginSuspects
      - FormIdSuspects
      - NamedRecords
      - Performance       # Extended metrics
      - GameHints
      - Footer
    Formatting:
      # Same as Enhanced
```

## Performance Considerations

1. **Lazy Loading**: Load section generators only when needed
2. **Parallel Processing**: Generate independent sections in parallel
3. **Caching**: Cache expensive operations like file system checks
4. **Streaming**: Use streaming for large report generation

## Testing Strategy

1. **Unit Tests**: Test each section generator independently
2. **Integration Tests**: Test complete report generation
3. **Comparison Tests**: Compare output with Python version
4. **Performance Tests**: Ensure acceptable performance

## Migration Guide

For users transitioning from Python CLASSIC:

1. **Report Formats**: Three templates provide flexibility:
   - **Standard**: Similar to Python's basic output
   - **Enhanced**: Rich formatting with game hints (no FCX required)
   - **Advanced**: Full FCX mode features

2. **Feature Parity**: 
   - Game hints are available without FCX (matches Python)
   - All crash analysis features work identically
   - FCX mode adds only file system checks

3. **Automatic Selection**:
   - Complex crashes automatically use Enhanced format
   - FCX mode always uses Advanced format
   - Users can override via settings

4. **Output Compatibility**:
   - Reports contain the same information as Python
   - Markdown format for better readability
   - Can export to plain text if needed

## Conclusion

This unified approach provides:
- **Three-tier flexibility**: Basic, Enhanced, or Advanced reports based on user needs
- **Feature parity with Python**: Game hints available without FCX, matching original behavior
- **Better user experience**: Automatic format selection for optimal readability
- **Clear separation**: FCX features clearly isolated from core functionality
- **Type safety and performance**: Benefits of C# while maintaining compatibility
- **Future-proof design**: Easy to add new templates or modify existing ones

The three-tier template system gives users maximum flexibility:
- **Standard**: Quick, simple reports for basic analysis
- **Enhanced**: Professional reports with full analysis, no FCX required
- **Advanced**: Complete system analysis when FCX is enabled

This implementation leverages existing C# infrastructure while introducing patterns that make the codebase more maintainable and extensible than the original Python version.