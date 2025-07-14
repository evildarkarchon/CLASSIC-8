# CLASSIC-8 Report Template Feature Comparison

## Complete Feature Matrix

| Feature | Standard | Enhanced | Advanced (FCX) | Notes |
|---------|:--------:|:--------:|:--------------:|-------|
| **Basic Information** | | | | |
| File name and metadata | ✅ | ✅ | ✅ | |
| Error analysis | ✅ | ✅ | ✅ | |
| Crash generator version check | ✅ | ✅ | ✅ | |
| **Formatting** | | | | |
| Basic Markdown | ✅ | ❌ | ❌ | |
| Unicode boxes and icons | ❌ | ✅ | ✅ | |
| Emoji indicators | ❌ | ✅ | ✅ | |
| **Analysis Features** | | | | |
| Settings validation | ✅ | ✅ | ✅ | |
| Crash suspects | ✅ | ✅ | ✅ | |
| Priority grouping | ❌ | ✅ | ✅ | |
| Confidence percentages | ❌ | ✅ | ✅ | |
| Technical details | ❌ | ✅ | ✅ | |
| **Mod Analysis** | | | | |
| Basic mod conflicts | ✅ | ✅ | ✅ | |
| Categorized conflicts | ❌ | ✅ | ✅ | |
| Compatibility score | ❌ | ✅ | ✅ | |
| **Plugin Analysis** | | | | |
| Plugin list | ✅ | ✅ | ✅ | |
| Plugin limit warnings | ✅ | ✅ | ✅ | |
| Detailed plugin status | ❌ | ✅ | ✅ | |
| Dependencies and patches | ❌ | ✅ | ✅ | |
| **FormID Analysis** | | | | |
| Basic FormID suspects | ✅ | ✅ | ✅ | |
| Corruption type details | ❌ | ✅ | ✅ | |
| Related records | ❌ | ✅ | ✅ | |
| Analysis summary | ❌ | ✅ | ✅ | |
| **Named Records** | | | | |
| Basic listing | ✅ | ✅ | ✅ | |
| Grouped by type | ❌ | ✅ | ✅ | |
| Override information | ❌ | ✅ | ✅ | |
| **Performance Metrics** | | | | |
| Processing time | ❌ | ✅ | ✅ | |
| Pattern match stats | ❌ | ✅ | ✅ | |
| File I/O metrics | ❌ | ❌ | ✅ | FCX only |
| Worker thread info | ❌ | ❌ | ✅ | FCX only |
| **FCX-Specific Features** | | | | |
| FCX mode notice | ❌ | ❌ | ✅ | |
| Main files integrity check | ❌ | ❌ | ✅ | |
| Game files validation | ❌ | ❌ | ✅ | |
| Extended performance data | ❌ | ❌ | ✅ | |
| **User Guidance** | | | | |
| Basic recommendations | ✅ | ✅ | ✅ | |
| Executive summary | ❌ | ✅ | ✅ | |
| Top actions list | ❌ | ✅ | ✅ | |
| Stability score | ❌ | ✅ | ✅ | |
| Risk assessment | ❌ | ✅ | ✅ | |
| **Game Hints** | ❌ | ✅ | ✅ | Not FCX-specific |
| FCX mode suggestion | ❌ | ✅ | ❌ | Only in Enhanced |

## Key Takeaways

1. **Game Hints are NOT FCX-specific** - Available in both Enhanced and Advanced templates
2. **Enhanced Template** provides ~90% of Advanced features without requiring FCX mode
3. **Only FCX-exclusive features** are:
   - File system integrity checks (main files, game files)
   - Extended performance metrics (file I/O, worker threads)
   - FCX mode notice

## Template Selection Guide

```
User wants basic analysis → Standard Template
User wants detailed analysis without FCX → Enhanced Template
User has FCX enabled → Advanced Template
```

## File Size Comparison

- **Standard**: ~3-5 KB (minimal formatting, essential data)
- **Enhanced**: ~8-12 KB (rich formatting, full analysis, game hints)
- **Advanced**: ~15-25 KB (includes FCX file validation data)