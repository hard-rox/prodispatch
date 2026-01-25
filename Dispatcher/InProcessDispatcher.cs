using Prodispatch.Abstractions.Commands;
using Prodispatch.Abstractions.Dispatcher;
using Prodispatch.Abstractions.Notifications;
using Prodispatch.Abstractions.Pipeline;
using Prodispatch.Abstractions.Queries;
using Prodispatch.ServiceFactory;

namespace Prodispatch.Dispatcher;

/// <summary>
/// In-process dispatcher implementation.
/// </summary>
public class InProcessDispatcher : IDispatcher
{
    private readonly IServiceFactory _serviceFactory;

    public InProcessDispatcher(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(object));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        // Build the pipeline
        Func<ICommand, CancellationToken, Task> pipeline = async (cmd, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
                throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            await (Task)handleMethod.Invoke(handler, [cmd, ct])!;
        };

        // If we have behaviors, wrap them
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
                        .Invoke(currentBehavior, [cmd, ct, previousPipeline])!;
            }

            await pipelineWithReturn(command, cancellationToken);
        }
        else
        {
            await pipeline(command, cancellationToken);
        }
    }

    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(queryType, typeof(TResult));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        // Build the pipeline
        Func<IQuery<TResult>, CancellationToken, Task<TResult>> pipeline = async (q, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
                throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            return (TResult)await (Task<TResult>)handleMethod.Invoke(handler, [q, ct])!;
        };

        // Wrap with behaviors in reverse order
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var currentBehavior = behavior;
            var previousPipeline = pipeline;

            pipeline = async (q, ct) =>
                await (Task<TResult>)currentBehavior.GetType()
                    .GetMethod("HandleAsync")!
                    .Invoke(currentBehavior, [q, ct, previousPipeline])!;
        }

        return await pipeline(query, cancellationToken);
    }

    public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = _serviceFactory.GetInstances(handlerType);

        var tasks = new List<Task>();
        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
                throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            var task = (Task)handleMethod.Invoke(handler, [notification, cancellationToken])!;
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceFactory.GetInstance(handlerType);

        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, typeof(TResult));
        var behaviors = _serviceFactory.GetInstances(pipelineType).ToList();

        // Build the pipeline
        Func<ICommand<TResult>, CancellationToken, Task<TResult>> pipeline = async (cmd, ct) =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
                throw new InvalidOperationException($"Handler {handlerType.Name} has no HandleAsync method");

            return (TResult)await (Task<TResult>)handleMethod.Invoke(handler, [cmd, ct])!;
        };

        // Wrap with behaviors in reverse order
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var currentBehavior = behavior;
            var previousPipeline = pipeline;

            pipeline = async (cmd, ct) =>
                await (Task<TResult>)currentBehavior.GetType()
                    .GetMethod("HandleAsync")!
                    .Invoke(currentBehavior, [cmd, ct, previousPipeline])!;
        }

        return await pipeline(command, cancellationToken);
    }
}
