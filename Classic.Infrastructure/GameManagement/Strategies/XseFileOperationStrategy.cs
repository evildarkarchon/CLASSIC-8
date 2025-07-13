using System.IO.Abstractions;
using Serilog;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Strategy for handling XSE (Script Extender) files.
/// </summary>
public class XseFileOperationStrategy : FileOperationStrategyBase
{
    public override string Category => "XSE";

    public override string[] FilePatterns => [
        "*.dll", "*.exe", "*.log",
        "f4se_*", "skse64_*", "sksevr_*"
    ];

    public XseFileOperationStrategy(IFileSystem fileSystem, ILogger logger)
        : base(fileSystem, logger)
    {
    }
}
