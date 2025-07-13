using System.IO.Abstractions;
using Serilog;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Strategy for handling ReShade files.
/// </summary>
public class ReshadeFileOperationStrategy : FileOperationStrategyBase
{
    public override string Category => "RESHADE";

    public override string[] FilePatterns => [
        "dxgi.dll", "d3d11.dll", "d3d9.dll", "opengl32.dll",
        "reshade.ini", "ReShade.ini"
    ];

    public ReshadeFileOperationStrategy(IFileSystem fileSystem, ILogger logger)
        : base(fileSystem, logger)
    {
    }
}
