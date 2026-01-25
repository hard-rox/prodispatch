namespace ProDispatch.ServiceFactory;

/// <summary>
/// Factory for resolving service instances.
/// </summary>
public interface IServiceFactory
{
    /// <summary>Gets a single instance for the given service type.</summary>
    /// <param name="serviceType">Type to resolve.</param>
    /// <returns>Resolved instance.</returns>
    object GetInstance(Type serviceType);

    /// <summary>Gets all registered instances for the given service type.</summary>
    /// <param name="serviceType">Type to resolve.</param>
    /// <returns>Sequence of resolved instances.</returns>
    IEnumerable<object> GetInstances(Type serviceType);
}
