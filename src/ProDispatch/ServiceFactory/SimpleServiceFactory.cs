namespace ProDispatch.ServiceFactory;

/// <summary>
/// Simple manual service factory implementation.
/// Can be wired to a DI container later.
/// </summary>
public class SimpleServiceFactory : IServiceFactory
{
    private readonly Dictionary<Type, List<Func<object>>> _serviceFactories = [];

    /// <summary>Registers a factory for a specific service type.</summary>
    /// <typeparam name="T">Service contract.</typeparam>
    /// <param name="factory">Factory delegate.</param>
    public void Register<T>(Func<object> factory) where T : class
    {
        var type = typeof(T);
        if (!_serviceFactories.ContainsKey(type))
        {
            _serviceFactories[type] = [];
        }

        _serviceFactories[type].Add(factory);
    }

    /// <summary>Registers a factory for a specific service type.</summary>
    /// <param name="serviceType">Service contract.</param>
    /// <param name="factory">Factory delegate.</param>
    public void Register(Type serviceType, Func<object> factory)
    {
        if (!_serviceFactories.ContainsKey(serviceType))
        {
            _serviceFactories[serviceType] = [];
        }

        _serviceFactories[serviceType].Add(factory);
    }

    /// <summary>Resolves the first registered instance for a type.</summary>
    /// <param name="serviceType">Type to resolve.</param>
    /// <returns>Resolved instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type is not registered.</exception>
    public object GetInstance(Type serviceType)
    {
        if (_serviceFactories.TryGetValue(serviceType, out var factories) && factories.Count > 0)
        {
            return factories[0]();
        }

        throw new InvalidOperationException($"No service registered for type {serviceType.Name}");
    }

    /// <summary>Resolves all registered instances for a type.</summary>
    /// <param name="serviceType">Type to resolve.</param>
    /// <returns>Resolved instances, or an empty sequence if none are registered.</returns>
    public IEnumerable<object> GetInstances(Type serviceType)
    {
        if (_serviceFactories.TryGetValue(serviceType, out var factories))
        {
            return factories.Select(f => f()).ToList();
        }

        return [];
    }
}
