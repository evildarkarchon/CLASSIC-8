namespace Classic.Core.Interfaces;

public interface IGlobalRegistry
{
    T GetService<T>() where T : class;
    void RegisterService<T>(T service) where T : class;

    void RegisterService<TInterface, TImplementation>(TImplementation service)
        where TInterface : class
        where TImplementation : class, TInterface;

    bool IsServiceRegistered<T>() where T : class;
    void ClearServices();

    /// <summary>
    /// Gets or sets whether the application is running in VR mode.
    /// </summary>
    bool IsVrMode { get; set; }

    /// <summary>
    /// Gets or sets the current game name.
    /// </summary>
    string CurrentGame { get; set; }
}
