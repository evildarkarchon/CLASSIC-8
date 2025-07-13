using System.CommandLine;
using Classic.CLI.Commands;

namespace Classic.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("CLASSIC-8 - Crash Log Analysis and Game Scanner Tool");

        // Add commands
        rootCommand.AddCommand(new ScanLogsCommand());
        rootCommand.AddCommand(new ScanGameCommand());
        rootCommand.AddCommand(new BatchCommand());
        rootCommand.AddCommand(new ValidateFilesCommand());
        rootCommand.AddCommand(new GenerateReportCommand());

        // Add global options  
        var versionOption = new Option<bool>(
            ["-V"],
            "Show version information");
        rootCommand.AddOption(versionOption);

        rootCommand.SetHandler((showVersion) =>
        {
            if (showVersion)
            {
                Console.WriteLine("CLASSIC-8 v1.0.0");
                Console.WriteLine("A C# port of the CLASSIC crash log analysis tool");
                return;
            }

            // Show help if no command specified
            rootCommand.Invoke("--help");
        }, versionOption);

        return await rootCommand.InvokeAsync(args);
    }
}
