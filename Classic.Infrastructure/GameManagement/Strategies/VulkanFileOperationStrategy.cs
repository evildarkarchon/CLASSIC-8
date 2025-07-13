using System.IO.Abstractions;
using Serilog;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Strategy for handling Vulkan files.
/// </summary>
public class VulkanFileOperationStrategy : FileOperationStrategyBase
{
    public override string Category => "VULKAN";

    public override string[] FilePatterns => [
        "vulkan-1.dll", "vulkan*.dll"
    ];

    public VulkanFileOperationStrategy(IFileSystem fileSystem, ILogger logger)
        : base(fileSystem, logger)
    {
    }
}
