using System.Collections.Concurrent;
using Classic.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Classic.Infrastructure.Registry;

public class GlobalRegistry(IServiceProvider? serviceProvider = null, ILogger? logger = null) : IGlobalRegistry
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    public T GetService<T>() where T : class
    {
        var serviceType = typeof(T);

        // First check our internal registry
        if (_services.TryGetValue(serviceType, out var service)) return (T)service;

        // If we have a service provider, try to get the service from it
        if (serviceProvider != null)
        {
            var resolvedService = serviceProvider.GetService<T>();
            if (resolvedService != null)
            {
                // Cache it for future use
                _services.TryAdd(serviceType, resolvedService);
                return resolvedService;
            }
        }

        logger?.Warning("Service of type {ServiceType} not found in registry", serviceType.Name);
        throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
    }

    public void RegisterService<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);

        var serviceType = typeof(T);
        _services.AddOrUpdate(serviceType, service, (key, oldValue) => service);

        logger?.Debug("Registered service of type {ServiceType}", serviceType.Name);
    }

    public void RegisterService<TInterface, TImplementation>(TImplementation service)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(service);

        var interfaceType = typeof(TInterface);
        _services.AddOrUpdate(interfaceType, service, (key, oldValue) => service);

        logger?.Debug("Registered service {ImplementationType} for interface {InterfaceType}",
            typeof(TImplementation).Name, interfaceType.Name);
    }

    public bool IsServiceRegistered<T>() where T : class
    {
        var serviceType = typeof(T);
        return _services.ContainsKey(serviceType) ||
               serviceProvider?.GetService<T>() != null;
    }

    public void ClearServices()
    {
        _services.Clear();
        logger?.Debug("Cleared all registered services");
    }
}
