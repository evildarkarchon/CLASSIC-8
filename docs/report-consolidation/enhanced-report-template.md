╔════════════════════════════════════════════════════════════════════════════════╗
║  CLASSIC-8 Crash Log Analysis Report                                             ║
║  Comprehensive Analysis and Recommendations                                      ║
╚════════════════════════════════════════════════════════════════════════════════╝

📂 **File:** {{FileName}}  
🕐 **Generated:** {{GeneratedDate}}  
⚡ **Processing Time:** {{ProcessingTimeSeconds}}s  
🎮 **Game:** {{GameName}}  
🔧 **CLASSIC Version:** {{ClassicVersion}}

📖 **How to read this report:**
• 🚨 Critical issues require immediate attention
• ⚠️ Warnings should be addressed for stability
• 💡 Recommendations can improve your experience
• 🔗 Links provide additional information and solutions

═══════════════════════════════════════════════════════════════════════════════════

* NOTICE: FCX MODE IS DISABLED. YOU CAN ENABLE IT TO DETECT PROBLEMS IN YOUR MOD & GAME FILES *
[ FCX Mode can be enabled in the exe or CLASSIC Settings.yaml located in your CLASSIC folder. ]

═══════════════════════════════════════════════════════════════════════════════════

## 🔍 ERROR ANALYSIS

**Main Error:** {{MainError}}  
**Crash Generator:** {{CrashGenName}} v{{CrashGenVersion}}  
**Game Version:** {{GameVersion}}

{{#if IsOutdated}}
🔴 **CRITICAL:** Your {{CrashGenName}} is outdated!  
**Current Version:** {{CrashGenVersion}}  
**Latest Version:** {{LatestCrashGenVersion}}  
**Action Required:** Update immediately from Nexus Mods
{{else}}
✅ **{{CrashGenName}} Status:** Up to date
{{/if}}

{{#if CrashAddress}}
**Crash Address:** `{{CrashAddress}}`  
**Probable Cause:** {{ProbableCause}}
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## ⚙️ SETTINGS VALIDATION

{{#each SettingsIssues}}
### {{SeverityIcon}} {{SettingName}}
- **Status:** {{Status}}
- **Current:** `{{CurrentValue}}`
- **Expected:** `{{ExpectedValue}}`
- **Impact:** {{Description}}
{{#if Recommendation}}
- **Fix:** {{Recommendation}}
{{/if}}

{{/each}}

{{#unless SettingsIssues}}
✅ All critical settings are properly configured.
{{/unless}}

───────────────────────────────────────────────────────────────────────────────────

## 🔥 CRASH CAUSE ANALYSIS

{{#if CrashSuspects}}
{{#each CrashSuspectsByPriority}}
### {{SeverityBadge}} Priority {{Priority}}: {{Name}}

**Confidence:** {{ConfidencePercentage}}%  
**Category:** {{Category}}

**Description:** {{Description}}

{{#if Evidence}}
**Evidence Found:**
{{#each EvidenceItems}}
• {{this}}
{{/each}}
{{/if}}

**Recommended Actions:**
{{#each Recommendations}}
{{@index+1}}. {{this}}
{{/each}}

{{#if TechnicalDetails}}
<details>
<summary>Technical Details</summary>

{{TechnicalDetails}}

</details>
{{/if}}

---
{{/each}}
{{else}}
ℹ️ No specific crash patterns identified. This may be a unique or complex issue.
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## ⚔️ MOD COMPATIBILITY ANALYSIS

{{#each ModConflictCategories}}
### {{CategoryIcon}} {{CategoryName}}

{{#each Conflicts}}
**{{ModName}}** v{{ModVersion}}
{{#if ConflictType}}
- Conflict Type: {{ConflictType}}
{{/if}}
{{#if ConflictsWith}}
- Conflicts With: {{#join ConflictsWith ", "}}{{this}}{{/join}}
{{/if}}
{{#if KnownIssues}}
- Known Issues: {{KnownIssues}}
{{/if}}
{{#if Solution}}
- **Solution:** {{Solution}}
{{/if}}
{{#if AlternativeMods}}
- **Alternatives:** {{#join AlternativeMods ", "}}{{this}}{{/join}}
{{/if}}

{{/each}}
{{/each}}

{{#unless ModConflictCategories}}
✅ No mod conflicts or incompatibilities detected.
{{/unless}}

**Compatibility Score:** {{CompatibilityScore}}/100

───────────────────────────────────────────────────────────────────────────────────

## 🔌 PLUGIN ANALYSIS

{{#if PluginLimitStatus}}
### Plugin Limit Status
- **Total Plugins:** {{TotalPlugins}}
- **Light Plugins:** {{LightPlugins}}
- **Regular Plugins:** {{RegularPlugins}}
- **FF-Prefix Plugins:** {{FFPrefixCount}}

{{#if ExceedsLimit}}
🔴 **CRITICAL:** Plugin limit exceeded! Game instability likely.
**Action Required:** Remove or merge plugins immediately.
{{/if}}
{{/if}}

### Problematic Plugins Detected
{{#if ProblematicPlugins}}
{{#each ProblematicPlugins}}
`[{{LoadOrderHex}}]` **{{Name}}**
- Status: {{StatusBadge}} {{Status}}
- Issues: {{#join Issues ", "}}{{this}}{{/join}}
{{#if Dependencies}}
- Missing Dependencies: {{#join Dependencies ", "}}{{this}}{{/join}}
{{/if}}
{{#if Patches}}
- Available Patches: {{#join Patches ", "}}{{this}}{{/join}}
{{/if}}

{{/each}}
{{else}}
✅ No problematic plugins detected.
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## 🔢 FORMID ANALYSIS

{{#if FormIdSuspects}}
### Suspicious FormIDs Detected
{{#each FormIdSuspects}}
**FormID:** `{{FormIdHex}}` [`{{PluginIndexHex}}:{{LocalFormIdHex}}`]
- Source Plugin: `{{PluginName}}`
- Record Type: `{{FormType}}`
- Status: {{StatusIcon}} {{Status}}
{{#if CorruptionType}}
- Corruption Type: {{CorruptionType}}
{{/if}}
{{#if RelatedRecords}}
- Related Records: {{#join RelatedRecords ", "}}{{this}}{{/join}}
{{/if}}

{{/each}}

**Analysis:** {{FormIdAnalysisSummary}}
{{else}}
✅ No suspicious FormIDs detected.
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## 📜 NAMED RECORDS ANALYSIS

{{#if NamedRecords}}
{{#each NamedRecordsByType}}
### {{RecordType}}
{{#each Records}}
- **{{RecordName}}** [`{{FormIdHex}}`]
  Plugin: {{PluginName}}
  {{#if Overrides}}Overridden by: {{#join Overrides ", "}}{{this}}{{/join}}{{/if}}
  {{#if Issues}}Issues: {{#join Issues ", "}}{{this}}{{/join}}{{/if}}

{{/each}}
{{/each}}
{{else}}
ℹ️ No named records found in crash context.
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## 📊 PERFORMANCE ANALYSIS

**Scan Performance:** {{PerformanceIcon}} {{PerformanceAssessment}}  
**Processing Time:** {{ProcessingTimeSeconds}} seconds  
**Files Analyzed:** {{TotalFilesAnalyzed}}

### Analysis Breakdown
- Pattern Matching: {{PatternMatchCount}} patterns checked
- Suspect Detection: {{SuspectsFound}} suspects identified
- Plugin Analysis: {{PluginsAnalyzed}} plugins analyzed
- FormID Checks: {{FormIdsChecked}} FormIDs validated

{{#if PerformanceRecommendations}}
### Performance Notes
{{#each PerformanceRecommendations}}
• {{this}}
{{/each}}
{{/if}}

───────────────────────────────────────────────────────────────────────────────────

## 💡 GAME TIPS & HINTS

{{#each GameHints}}
💡 {{this}}

{{/each}}

───────────────────────────────────────────────────────────────────────────────────

## 📋 EXECUTIVE SUMMARY

### Issue Severity Breakdown
- 🔴 Critical Issues: {{CriticalCount}}
- 🟠 High Priority: {{HighCount}}
- 🟡 Medium Priority: {{MediumCount}}
- 🟢 Low Priority: {{LowCount}}

### Top 3 Actions Required
{{#each TopActions}}
{{@index+1}}. {{Action}} - {{Impact}}
{{/each}}

### Stability Assessment
**Overall Stability Score:** {{StabilityScore}}/100  
**Risk Level:** {{RiskLevel}}  
**Recommendation:** {{OverallRecommendation}}

{{#if EnableFCXSuggestion}}
💡 **Pro Tip:** Enable FCX Mode for deeper analysis of your game files and mod conflicts. This can help identify issues that standard scanning might miss.
{{/if}}

═══════════════════════════════════════════════════════════════════════════════════

🔧 Generated by {{ClassicVersion}}  
📖 Documentation: https://docs.google.com/document/d/17FzeIMJ256xE85XdjoPvv_Zi3C5uHeSTQh6wOZugs4c  
👥 Community: r/FalloutMods, Nexus Forums  
📅 Report generated: {{GeneratedDateTime}}

Thank you for using CLASSIC-8!