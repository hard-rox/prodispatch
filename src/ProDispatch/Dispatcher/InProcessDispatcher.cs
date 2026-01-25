using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Dispatcher;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Abstractions.Queries;
using ProDispatch.ServiceFactory;

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

    /// <summary>Sends a command without a result.</summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(object));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        Func<ICommand, CancellationToken, Task> pipeline = async (cmd, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            await (Task)handleMethod.Invoke(handler, new object?[] { cmd, ct })!;
        };

        if (behaviors.Count > 0)
        {
            Func<ICommand, CancellationToken, Task<object?>> pipelineWithReturn = async (cmd, ct) =>
            {
                await pipeline(cmd, ct);
                return null;
            };

            foreach (var behavior in behaviors.AsEnumerable().Reverse())
            {
                var currentBehavior = behavior;
                var previousPipeline = pipelineWithReturn;

                pipelineWithReturn = async (cmd, ct) =>
                    await (Task<object?>)currentBehavior.GetType()
                        .GetMethod("HandleAsync")!
                        .Invoke(currentBehavior, new object?[] { cmd, ct, previousPipeline })!;
            }

            await pipelineWithReturn(command, cancellationToken);
        }
        else
        {
            await pipeline(command, cancellationToken);
        }
    }

    /// <summary>Sends a command with a result.</summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(TResult));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        Func<ICommand<TResult>, CancellationToken, Task<TResult>> pipeline = async (cmd, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            return (TResult)await (Task<TResult>)handleMethod.Invoke(handler, new object?[] { cmd, ct })!;
        };

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var currentBehavior = behavior;
            var previousPipeline = pipeline;

            pipeline = async (cmd, ct) =>
                await (Task<TResult>)currentBehavior.GetType()
                    .GetMethod("HandleAsync")!
                    .Invoke(currentBehavior, new object?[] { cmd, ct, previousPipeline })!;
        }

        return await pipeline(command, cancellationToken);
    }

    /// <summary>Sends a query with a result.</summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="query">Query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(queryType, typeof(TResult));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        Func<IQuery<TResult>, CancellationToken, Task<TResult>> pipeline = async (q, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            return (TResult)await (Task<TResult>)handleMethod.Invoke(handler, new object?[] { q, ct })!;
        };

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var currentBehavior = behavior;
            var previousPipeline = pipeline;

            pipeline = async (q, ct) =>
                await (Task<TResult>)currentBehavior.GetType()
                    .GetMethod("HandleAsync")!
                    .Invoke(currentBehavior, new object?[] { q, ct, previousPipeline })!;
        }

        return await pipeline(query, cancellationToken);
    }

    /// <summary>Publishes a notification to all registered handlers.</summary>
    /// <param name="notification">Notification payload.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = _serviceFactory.GetInstances(handlerType);

        var tasks = new List<Task>();
        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            var task = (Task)handleMethod.Invoke(handler, new object?[] { notification, cancellationToken })!;
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
}
