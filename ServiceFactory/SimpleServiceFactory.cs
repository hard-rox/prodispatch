namespace Prodispatch.ServiceFactory;

/// <summary>
/// Simple manual service factory implementation.
/// Can be wired to a DI container later.
/// </summary>
public class SimpleServiceFactory : IServiceFactory
{
    private readonly Dictionary<Type, List<Func<object>>> _serviceFactories = [];

    public void Register<T>(Func<object> factory) where T : class
    {
        var type = typeof(T);
        if (!_serviceFactories.ContainsKey(type))
        {
            _serviceFactories[type] = [];
        }

        _serviceFactories[type].Add(factory);
    }

    public void Register(Type serviceType, Func<object> factory)
    {
        if (!_serviceFactories.ContainsKey(serviceType))
        {
            _serviceFactories[serviceType] = [];
        }

        _serviceFactories[serviceType].Add(factory);
    }

    public object GetInstance(Type serviceType)
    {
        if (_serviceFactories.TryGetValue(serviceType, out var factories) && factories.Count > 0)
        {
            return factories[0]();
        }

        throw new InvalidOperationException($"No service registered for type {serviceType.Name}");
    }

    public IEnumerable<object> GetInstances(Type serviceType)
    {
        if (_serviceFactories.TryGetValue(serviceType, out var factories))
        {
            return factories.Select(f => f()).ToList();
        }

        return [];
    }
}
