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
}
