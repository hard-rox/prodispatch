using System.Reflection;
using ProDispatch.Abstractions.Requests;
using ProDispatch.ServiceFactory;
using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Dispatcher;

/// <summary>
/// In-process dispatcher implementation.
/// </summary>
public class InProcessDispatcher : IDispatcher
{
    private readonly IServiceFactory _serviceFactory;

    /// <summary>Initializes a new instance of the dispatcher.</summary>
    /// <param name="serviceFactory">Factory used to resolve handlers and behaviors.</param>
    public InProcessDispatcher(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    /// <summary>Sends a request and returns a result.</summary>
    /// <typeparam name="TResponse">Request result type.</typeparam>
    /// <param name="request">Request to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        // Try to resolve IRequestHandler first
        object? handler = null;
        try
        {
            handler = _serviceFactory.GetInstance(handlerType);
        }
        catch (InvalidOperationException)
        {
            // Fallback: Try to resolve specific command/query handler types
            handler = TryResolveSpecificHandler(request, requestType, typeof(TResponse));
        }

        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for request type {requestType.Name}");
        }

        // Collect behaviors - for Unit commands, also include object-typed behaviors
        List<(object behavior, bool isObjectTyped)> behaviors = [];
        Type pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        
        foreach (var b in _serviceFactory.GetInstances(pipelineType))
        {
            if (ShouldApplyBehavior(b, request))
                behaviors.Add((b, false));
        }
        
        if (typeof(TResponse) == typeof(UnitType))
        {
            Type objectPipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(object));
            foreach (var b in _serviceFactory.GetInstances(objectPipelineType))
            {
                if (ShouldApplyBehavior(b, request))
                    behaviors.Add((b, true));
            }
        }

        Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> pipeline = async (req, ct) =>
        {
            MethodInfo handleMethod = handler.GetType().GetMethod("HandleAsync")
                                      ?? throw new InvalidOperationException($"Handler {handler.GetType().Name} has no HandleAsync method");

            var result = handleMethod.Invoke(handler, [req, ct]);
            
            // Handle both Task<TResponse> and Task (for void commands)
            if (result is Task<TResponse> taskWithResult)
            {
                return await taskWithResult;
            }
            else if (result is Task task)
            {
                await task;
                // For void commands, return Unit.Value cast to TResponse
                return (TResponse)(object)UnitType.Value;
            }
            
            throw new InvalidOperationException($"Handler method returned unexpected type: {result?.GetType().Name ?? "null"}");
        };

        foreach (var (behavior, isObjectTyped) in behaviors.AsEnumerable().Reverse())
        {
            var currentBehavior = behavior;
            Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> previousPipeline = pipeline;

            if (isObjectTyped && typeof(TResponse) == typeof(UnitType))
            {
                // Create adapter for object-typed behaviors when response is Unit
                pipeline = async (req, ct) =>
                {
                    MethodInfo? behaviorMethod = currentBehavior.GetType().GetMethod("HandleAsync");
                    if (behaviorMethod == null)
                    {
                        throw new InvalidOperationException($"Behavior {currentBehavior.GetType().Name} has no HandleAsync method");
                    }

                    // Create an adapter delegate that converts Unit to object
                    Func<object, CancellationToken, Task<object>> objectNext = async (objReq, objCt) =>
                    {
                        var unitResult = await previousPipeline((IRequest<TResponse>)objReq, objCt);
                        return (object?)unitResult ?? new object();
                    };

                    var behaviorResult = behaviorMethod.Invoke(currentBehavior, [req, ct, objectNext]);
                    
                    if (behaviorResult is Task<object> objectTask)
                    {
                        await objectTask;
                        return (TResponse)(object)UnitType.Value;
                    }
                    
                    throw new InvalidOperationException($"Behavior method returned unexpected type: {behaviorResult?.GetType().Name ?? "null"}");
                };
            }
            else
            {
                // Normal case - behavior response type matches
                pipeline = async (req, ct) =>
                {
                    MethodInfo? behaviorMethod = currentBehavior.GetType().GetMethod("HandleAsync");
                    if (behaviorMethod == null)
                    {
                        throw new InvalidOperationException($"Behavior {currentBehavior.GetType().Name} has no HandleAsync method");
                    }

                    var behaviorResult = behaviorMethod.Invoke(currentBehavior, [req, ct, previousPipeline]);
                    
                    if (behaviorResult is Task<TResponse> typedTask)
                    {
                        return await typedTask;
                    }
                    
                    throw new InvalidOperationException($"Behavior method returned unexpected type: {behaviorResult?.GetType().Name ?? "null"}");
                };
            }
        }

        return pipeline(request, cancellationToken);
    }

    /// <summary>
    /// Attempts to resolve a handler using specific command or query handler interfaces.
    /// </summary>
    private object? TryResolveSpecificHandler<TResponse>(IRequest<TResponse> request, Type requestType, Type responseType)
    {
        // Check if it's a command without result
        if (request is ICommand && typeof(TResponse) == typeof(UnitType))
        {
            Type commandHandlerType = typeof(ICommandHandler<>).MakeGenericType(requestType);
            try
            {
                return _serviceFactory.GetInstance(commandHandlerType);
            }
            catch (InvalidOperationException) { }
        }
        
        // Check if it's a command with result
        if (request.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)))
        {
            Type commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType);
            try
            {
                return _serviceFactory.GetInstance(commandHandlerType);
            }
            catch (InvalidOperationException) { }
        }
        
        // Check if it's a query
        if (request.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            Type queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType);
            try
            {
                return _serviceFactory.GetInstance(queryHandlerType);
            }
            catch (InvalidOperationException) { }
        }
        
        return null;
    }

    /// <summary>Sends a command without a result.</summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        await Send<UnitType>(command, cancellationToken);
    }

    /// <summary>Sends a command with a result.</summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Send(command, cancellationToken);
    }

    /// <summary>Sends a query with a result.</summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="query">Query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Send(query, cancellationToken);
    }

    /// <summary>Publishes a notification to all registered handlers.</summary>
    /// <param name="notification">Notification payload.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        Type notificationType = notification.GetType();
        Type handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        IEnumerable<object> handlers = _serviceFactory.GetInstances(handlerType);

        List<Task> tasks = [];
        foreach (var handler in handlers)
        {
            MethodInfo handleMethod = handlerType.GetMethod("HandleAsync")
                                      ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            Task task = (Task)handleMethod.Invoke(handler, [notification, cancellationToken])!;
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Determines whether a behavior should be applied to a request based on its scope markers.
    /// </summary>
    private static bool ShouldApplyBehavior<TResponse>(object behavior, IRequest<TResponse> request)
    {
        bool isCommand = request is ICommand || (request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
        bool isQuery = request is IQuery<TResponse> || (request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)));

        // Check if behavior implements scoped marker interfaces
        bool isCommandBehavior = behavior is ICommandPipelineBehavior;
        bool isQueryBehavior = behavior is IQueryPipelineBehavior;

        // If no scope markers, apply to all
        if (!isCommandBehavior && !isQueryBehavior)
            return true;

        // Apply based on scope
        if (isCommandBehavior && isCommand)
            return true;

        if (isQueryBehavior && isQuery)
            return true;

        return false;
    }
}
