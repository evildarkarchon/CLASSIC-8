# Classic.Report

The unified report generation system for the CLASSIC C# port, implementing the three-tier template system for crash log analysis reports.

## Overview

This library implements Phase 1 of the [Unified Report Writing Implementation Strategy](../docs/report-consolidation/classic-report-implementation.md), providing a flexible foundation for generating crash analysis reports in three different formats:

- **Standard**: Basic format for quick analysis with minimal formatting
- **Enhanced**: Advanced formatting with game hints (no FCX required)  
- **Advanced**: Full FCX mode with file system checks and extended metrics

## Architecture

### Core Components

- **Strategy Pattern**: `IReportStrategy` implementations for each report format
- **Section Generators**: Modular components for generating specific report sections
- **Unified Generator**: `UnifiedReportGenerator` that orchestrates the entire process
- **Template Selection**: Intelligent format selection based on crash complexity

### Key Features

✅ **Three-Tier Template System**
- Standard, Enhanced, and Advanced report formats
- Automatic format selection based on crash complexity
- Manual format override support

✅ **Strategy Pattern Implementation**
- `StandardReportStrategy` - Basic reports
- `EnhancedReportStrategy` - Rich formatting with game hints
- `AdvancedReportStrategy` - FCX mode with file validation

✅ **Modular Section Generators**
- Header, Error, Suspect, Settings, Plugin sections
- FormID analysis and Named Records
- Performance metrics and Game hints
- FCX-specific sections (placeholders for future implementation)

✅ **Dependency Injection Ready**
- Service registration extensions
- Factory pattern for strategy management
- Configurable options

## Usage

### Basic Setup

```csharp
// Register services in your DI container
services.AddClassicReporting();

// Use the unified generator
var generator = serviceProvider.GetRequiredService<IUnifiedReportGenerator>();

var options = new ReportOptions
{
    AutoSelectFormat = true,
    IncludeGameHints = true,
    IncludePerformanceMetrics = true
};

var report = await generator.GenerateReportAsync(analysisResult, options);
```

### Custom Configuration

```csharp
services.AddClassicReporting(config =>
{
    config.DefaultOptions.AutoSelectFormat = true;
    config.EnableDetailedLogging = true;
    config.EnhancedFormatThreshold = 3;
});
```

### Manual Format Selection

```csharp
var options = new ReportOptions
{
    AutoSelectFormat = false,
    PreferredFormat = ReportTemplateType.Enhanced,
    FCXMode = false
};

var report = await generator.GenerateReportAsync(analysisResult, options);
```

### Different Report Types

```csharp
// Standard Report (basic formatting)
var standardOptions = new ReportOptions 
{ 
    PreferredFormat = ReportTemplateType.Standard 
};

// Enhanced Report (rich formatting with game hints)  
var enhancedOptions = new ReportOptions
{
    PreferredFormat = ReportTemplateType.Enhanced,
    IncludeGameHints = true,
    IncludePerformanceMetrics = true
};

// Advanced Report (FCX mode with file validation)
var advancedOptions = new ReportOptions
{
    FCXMode = true,
    IncludeGameHints = true,
    IncludePerformanceMetrics = true
};
```

## Implementation Status

### ✅ Phase 1 Complete
- [x] Core interfaces and base classes
- [x] Section generator infrastructure  
- [x] Three-tier strategy pattern framework
- [x] Template selection logic
- [x] Placeholder section generators

### 🚧 Future Phases

**Phase 2: Standard Report** (Week 3)
- Implement actual section generators
- Port crash suspect detection logic
- Basic plugin and FormID analysis

**Phase 3: Enhanced Report** (Week 4-5)  
- Rich formatting templates
- Priority grouping for suspects
- Executive summary generation
- Game hints loader implementation

**Phase 4: Advanced Report / FCX Mode** (Week 6-7)
- FCX mode handler implementation
- Main files validation
- Game files validation
- Extended performance metrics

## Project Structure

```
Classic.Report/
├── Extensions/              # Service registration
├── Factories/              # Strategy factory
├── Generators/             # Section generators
│   ├── ISectionGenerators.cs
│   ├── PlaceholderSectionGenerators.cs
│   └── UnifiedReportGenerator.cs
├── Interfaces/             # Core interfaces
│   ├── IReportStrategy.cs
│   ├── IReportStrategyFactory.cs
│   ├── ISectionGenerator.cs
│   └── IUnifiedReportGenerator.cs
├── Models/                 # Data models
│   ├── ReportFormat.cs
│   ├── ReportOptions.cs
│   └── ReportSections.cs
├── Services/               # Helper services
│   └── TemplateSelectionService.cs
└── Strategies/             # Strategy implementations
    ├── ReportStrategyBase.cs
    ├── StandardReportStrategy.cs
    ├── EnhancedReportStrategy.cs
    └── AdvancedReportStrategy.cs
```

## Dependencies

- **Classic.Core** - Core models and interfaces
- **Microsoft.Extensions.DependencyInjection** - Service container
- **Microsoft.Extensions.Logging** - Logging abstractions  
- **Serilog** - Structured logging

## Design Decisions

### 1. Renamed ReportFormat to ReportTemplateType
- Avoided naming conflict with existing `Classic.Core.Models.ReportFormat`
- Clearer semantic meaning for template selection

### 2. Placeholder Implementations
- All section generators are currently placeholders
- Properly typed interfaces for future implementation
- Uses actual model structures from Classic.Core

### 3. Composition Over Inheritance
- Section generators are composed rather than inherited
- Strategy pattern for different report formats
- Factory pattern for strategy management

### 4. Async Throughout
- All generators use async/await patterns
- Prepared for I/O operations and parallel processing
- Cancellation token support

## Integration

This library integrates with existing CLASSIC infrastructure:

- Uses `Classic.Core.Models.CrashLogAnalysisResult` as input
- Compatible with existing `IReportTemplate` from Classic.Core
- Extends `IReportGenerator` pattern with unified capabilities

## Next Steps

1. **Implement Real Section Generators** - Replace placeholders with actual logic
2. **Add Template System** - Create Markdown templates for each format
3. **FCX Mode Implementation** - Add file validation capabilities
4. **Testing** - Comprehensive unit and integration tests
5. **Performance Optimization** - Parallel section generation

---

*This implementation provides the foundation for a flexible, maintainable report generation system that improves upon the original Python CLASSIC while maintaining full feature parity.*
