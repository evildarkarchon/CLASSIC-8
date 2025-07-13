using Classic.Avalonia.Services;
using Classic.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.Avalonia.Extensions;

/// <summary>
/// Extension methods for registering Avalonia-specific services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Avalonia UI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAvaloniaUIServices(this IServiceCollection services)
    {
        // Register theme service
        services.AddSingleton<IThemeService, ThemeService>();

        // Register window state service
        services.AddSingleton<IWindowStateService, WindowStateService>();

        // Register drag and drop service
        services.AddSingleton<IDragDropService, DragDropService>();

        return services;
    }
}
