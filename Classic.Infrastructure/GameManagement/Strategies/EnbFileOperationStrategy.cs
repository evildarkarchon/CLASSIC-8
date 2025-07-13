using System.IO.Abstractions;
using Serilog;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Strategy for handling ENB files.
/// </summary>
public class EnbFileOperationStrategy : FileOperationStrategyBase
{
    public override string Category => "ENB";

    public override string[] FilePatterns => [
        "d3d11.dll", "d3d9.dll",
        "enbseries.ini", "enblocal.ini",
        "enbseries/*", "enbcache/*"
    ];

    public EnbFileOperationStrategy(IFileSystem fileSystem, ILogger logger)
        : base(fileSystem, logger)
    {
    }
}
