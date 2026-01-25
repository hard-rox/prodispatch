namespace Prodispatch.ServiceFactory;

/// <summary>
/// Factory for resolving service instances.
/// </summary>
public interface IServiceFactory
{
    object GetInstance(Type serviceType);

    IEnumerable<object> GetInstances(Type serviceType);
}
