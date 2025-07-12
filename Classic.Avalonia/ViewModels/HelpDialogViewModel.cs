using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace Classic.Avalonia.ViewModels;

public class HelpDialogViewModel : ViewModelBase
{
    private HelpTopic? _selectedTopic;
    private string _helpContent = string.Empty;
    
    public HelpDialogViewModel()
    {
        CloseCommand = ReactiveCommand.Create<Unit, Unit>(_ => Unit.Default);
        
        // Initialize help topics
        InitializeHelpTopics();
        
        // Select the first topic by default
        if (Topics.Count > 0)
        {
            SelectedTopic = Topics[0];
        }
    }
    
    public ObservableCollection<HelpTopic> Topics { get; } = new();
    
    public HelpTopic? SelectedTopic
    {
        get => _selectedTopic;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTopic, value);
            if (value != null)
            {
                HelpContent = value.Content;
            }
        }
    }
    
    public string HelpContent
    {
        get => _helpContent;
        set => this.RaiseAndSetIfChanged(ref _helpContent, value);
    }
    
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    private void InitializeHelpTopics()
    {
        Topics.Add(new HelpTopic("Getting Started",
            @"Welcome to CLASSIC-8!

CLASSIC-8 is a crash log analyzer for Bethesda games including:
• Fallout 4
• Fallout 4 VR
• Skyrim Special Edition
• Skyrim VR

To get started:
1. Select your staging mods folder (if using a mod manager)
2. Click 'Scan Crash Logs' to analyze crash logs
3. Review the results to identify potential mod conflicts"));

        Topics.Add(new HelpTopic("Main Options Tab",
            @"The Main Options tab contains the primary functionality:

Folder Selection:
• Staging Mods: Select your mod manager's staging folder
• Custom Scan: Add additional folders to scan for crash logs

Scan Operations:
• Scan Crash Logs: Analyzes crash log files
• Scan Game Files: Checks game file integrity

Settings:
• FCX Mode: Enable enhanced crash analysis
• Simplify Logs: Create simplified log summaries
• Update Check: Check for application updates
• VR Mode: Enable VR-specific features
• Show FormID Values: Display technical FormID data
• Move Invalid Logs: Organize invalid log files"));

        Topics.Add(new HelpTopic("File Backup Tab",
            @"The File Backup tab helps manage game modifications:

Backup Categories:
• XSE: Script Extender files
• RESHADE: ReShade graphics mod files
• VULKAN: Vulkan API files
• ENB: ENB graphics mod files

Operations:
• Backup: Create a backup of the files
• Restore: Restore files from backup
• Remove: Remove the modification files

Always backup files before making changes!"));

        Topics.Add(new HelpTopic("Articles Tab",
            @"The Articles tab provides quick access to helpful resources:

• Installation guides
• Setup tips and best practices
• Important patches
• Links to mod pages
• Community resources

Click any button to open the resource in your browser."));

        Topics.Add(new HelpTopic("Analyzing Results",
            @"When scan completes, CLASSIC-8 provides:

Scan Statistics:
• Total logs processed
• Success/failure rates
• Processing time
• Top mod conflicts

Understanding Results:
• High conflict counts may indicate problematic mods
• Failed scans may indicate corrupted log files
• Check the output folder for detailed reports"));

        Topics.Add(new HelpTopic("Troubleshooting",
            @"Common Issues:

Access Denied:
• Run as administrator if needed
• Check folder permissions

No Logs Found:
• Verify crash logs exist in the game's folder
• Check custom scan folder path

Slow Scanning:
• Large mod collections take longer
• Disable real-time antivirus scanning temporarily

For more help:
• Check the GitHub repository
• Visit the Nexus Mods page"));

        Topics.Add(new HelpTopic("Keyboard Shortcuts",
            @"Keyboard shortcuts for efficient use:

General:
• Ctrl+O: Open folder selection
• Ctrl+S: Start scan
• F1: Show this help
• Alt+F4: Exit application

Navigation:
• Tab: Move between controls
• Enter: Activate focused button
• Escape: Close dialogs"));
    }
}

public class HelpTopic
{
    public HelpTopic(string title, string content)
    {
        Title = title;
        Content = content;
    }
    
    public string Title { get; }
    public string Content { get; }
}