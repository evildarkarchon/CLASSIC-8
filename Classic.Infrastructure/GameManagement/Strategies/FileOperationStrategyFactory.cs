using Classic.Core.Interfaces;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Factory for creating and managing file operation strategies.
/// </summary>
public class FileOperationStrategyFactory : IFileOperationStrategyFactory
{
    private readonly Dictionary<string, IFileOperationStrategy> _strategies = new(StringComparer.OrdinalIgnoreCase);

    public FileOperationStrategyFactory(IEnumerable<IFileOperationStrategy> strategies)
    {
        foreach (var strategy in strategies)
        {
            RegisterStrategy(strategy);
        }
    }

    public IFileOperationStrategy? GetStrategy(string category)
    {
        _strategies.TryGetValue(category, out var strategy);
        return strategy;
    }

    public IEnumerable<string> GetAvailableCategories()
    {
        return _strategies.Keys.ToList();
    }

    public void RegisterStrategy(IFileOperationStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        _strategies[strategy.Category] = strategy;
    }
}
