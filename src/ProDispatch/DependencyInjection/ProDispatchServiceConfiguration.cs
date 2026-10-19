using System.Reflection;

namespace ProDispatch.DependencyInjection;

/// <summary>Configures handler discovery and ordered pipeline behaviors.</summary>
public sealed class ProDispatchServiceConfiguration
{
    internal List<Assembly> Assemblies { get; } = [];
    internal List<(Type ServiceType, Type ImplementationType)> Behaviors { get; } = [];

    /// <summary>Discovers handlers in the supplied assembly.</summary>
    /// <param name="assembly">Assembly containing request or notification handlers.</param>
    /// <returns>This configuration for chaining.</returns>
    public ProDispatchServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>Adds a closed pipeline behavior. The first added behavior runs outermost.</summary>
    /// <typeparam name="TBehavior">Concrete pipeline behavior implementation.</typeparam>
    /// <returns>This configuration for chaining.</returns>
    public ProDispatchServiceConfiguration AddBehavior<TBehavior>() where TBehavior : class =>
        AddBehavior(typeof(TBehavior));

    /// <summary>Adds a closed pipeline behavior. The first added behavior runs outermost.</summary>
    /// <param name="behaviorType">Concrete implementation of one or more closed pipeline contracts.</param>
    /// <returns>This configuration for chaining.</returns>
    public ProDispatchServiceConfiguration AddBehavior(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        Type[] contracts = behaviorType.GetInterfaces().Where(IsPipelineContract).ToArray();
        if (!behaviorType.IsClass || behaviorType.IsAbstract || behaviorType.ContainsGenericParameters || contracts.Length == 0)
        {
            throw new ArgumentException("A closed, concrete IPipelineBehavior implementation is required.", nameof(behaviorType));
        }

        foreach (Type contract in contracts)
        {
            Behaviors.Add((contract, behaviorType));
        }

        return this;
    }

    /// <summary>Adds an open generic pipeline behavior in request/response type-parameter order.</summary>
    /// <param name="behaviorType">Concrete open generic implementation of IPipelineBehavior&lt;,&gt;.</param>
    /// <returns>This configuration for chaining.</returns>
    public ProDispatchServiceConfiguration AddOpenBehavior(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        if (!behaviorType.IsClass || behaviorType.IsAbstract || !behaviorType.IsGenericTypeDefinition ||
            behaviorType.GetGenericArguments().Length != 2)
        {
            throw new ArgumentException("An open, concrete IPipelineBehavior implementation with two type parameters is required.", nameof(behaviorType));
        }

        Type[] parameters = behaviorType.GetGenericArguments();
        bool matchesContract = behaviorType.GetInterfaces().Any(contract =>
            IsPipelineContract(contract) && contract.GetGenericArguments().SequenceEqual(parameters));
        if (!matchesContract)
        {
            throw new ArgumentException("The behavior must implement IPipelineBehavior<TRequest, TResponse> in type-parameter order.", nameof(behaviorType));
        }

        Behaviors.Add((typeof(IPipelineBehavior<,>), behaviorType));
        return this;
    }

    private static bool IsPipelineContract(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
}