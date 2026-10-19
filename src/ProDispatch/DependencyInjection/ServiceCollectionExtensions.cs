using Microsoft.Extensions.DependencyInjection.Extensions;
using ProDispatch.DependencyInjection;
using ProDispatch.Dispatcher;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers ProDispatch with Microsoft dependency injection.</summary>
public static class ProDispatchServiceCollectionExtensions
{
    /// <summary>Registers a transient dispatcher and discovers handlers in configured assemblies.</summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="configure">Configures assembly scanning and explicit pipeline behaviors.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddProDispatch(
        this IServiceCollection services,
        Action<ProDispatchServiceConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        ProDispatchServiceConfiguration configuration = new();
        configure(configuration);
        if (configuration.Assemblies.Count == 0)
        {
            throw new InvalidOperationException("Register at least one assembly using RegisterServicesFromAssembly.");
        }

        // Load all types before changing the collection so discovery errors do not leave partial registrations.
        Type[] implementations = configuration.Assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        services.TryAddTransient<IDispatcher, InProcessDispatcher>();
        foreach (Type implementation in implementations)
        {
            foreach (Type contract in implementation.GetInterfaces().Where(IsHandlerContract))
            {
                ServiceDescriptor descriptor = ServiceDescriptor.Transient(contract, implementation);
                if (contract.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                {
                    services.TryAddEnumerable(descriptor);
                }
                else
                {
                    services.TryAdd(descriptor);
                }
            }
        }

        foreach (var (serviceType, implementationType) in configuration.Behaviors)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, implementationType));
        }

        return services;
    }

    private static bool IsHandlerContract(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        Type definition = type.GetGenericTypeDefinition();
        return definition == typeof(IRequestHandler<>) ||
               definition == typeof(IRequestHandler<,>) ||
               definition == typeof(ICommandHandler<>) ||
               definition == typeof(ICommandHandler<,>) ||
               definition == typeof(IQueryHandler<,>) ||
               definition == typeof(INotificationHandler<>);
    }
}