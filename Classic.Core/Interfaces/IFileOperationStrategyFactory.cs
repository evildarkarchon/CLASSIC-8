namespace Classic.Core.Interfaces;

/// <summary>
/// Factory for creating file operation strategies for different game file categories.
/// </summary>
public interface IFileOperationStrategyFactory
{
    /// <summary>
    /// Gets the strategy for the specified category.
    /// </summary>
    /// <param name="category">The file category (case insensitive)</param>
    /// <returns>The strategy for the category, or null if not found</returns>
    IFileOperationStrategy? GetStrategy(string category);

    /// <summary>
    /// Gets all available categories.
    /// </summary>
    /// <returns>Collection of available category names</returns>
    IEnumerable<string> GetAvailableCategories();

    /// <summary>
    /// Registers a strategy for a category.
    /// </summary>
    /// <param name="strategy">The strategy to register</param>
    void RegisterStrategy(IFileOperationStrategy strategy);
}
