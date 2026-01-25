using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Dispatcher;
using ProDispatch.ServiceFactory;

namespace ProDispatch.Benchmarks;

[MemoryDiagnoser]
public class DispatcherBenchmarks
{
    private InProcessDispatcher _commandNoPipelineDispatcher = null!;
    private InProcessDispatcher _commandOnePipelineDispatcher = null!;
    private InProcessDispatcher _commandMultiplePipelineDispatcher = null!;
    private BenchmarkCommand _command = null!;
    private InProcessDispatcher _notificationDispatcher = null!;
    private BenchmarkNotification _notification = null!;

    [GlobalSetup]
    public void Setup()
    {
        _commandNoPipelineDispatcher = BuildCommandDispatcherNoPipeline();
        _commandOnePipelineDispatcher = BuildCommandDispatcherOnePipeline();
        _commandMultiplePipelineDispatcher = BuildCommandDispatcherMultiplePipeline();
        _notificationDispatcher = BuildNotificationDispatcher();
        _command = new BenchmarkCommand();
        _notification = new BenchmarkNotification("bench");
    }

    [Benchmark]
    public Task SendCommandNoPipelineAsync() => _commandNoPipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task SendCommandOnePipelineAsync() => _commandOnePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task SendCommandMultiplePipelineAsync() => _commandMultiplePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task PublishNotificationAsync() => _notificationDispatcher.PublishAsync(_notification);

    private static InProcessDispatcher BuildCommandDispatcherNoPipeline()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        return new InProcessDispatcher(factory);
    }

    private static InProcessDispatcher BuildCommandDispatcherOnePipeline()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        factory.Register(typeof(IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        return new InProcessDispatcher(factory);
    }

    private static InProcessDispatcher BuildCommandDispatcherMultiplePipeline()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        factory.Register(typeof(IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        factory.Register(typeof(IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        return new InProcessDispatcher(factory);
    }

    private static InProcessDispatcher BuildNotificationDispatcher()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        factory.Register<INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        factory.Register<INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        return new InProcessDispatcher(factory);
    }

    private sealed record BenchmarkCommand : ICommand;

    private sealed record BenchmarkNotification(string Name) : INotification;

    private sealed class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
    {
        public Task HandleAsync(BenchmarkCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpBehavior : IPipelineBehavior<BenchmarkCommand, object>
    {
        public Task<object> HandleAsync(BenchmarkCommand request, CancellationToken cancellationToken, Func<BenchmarkCommand, CancellationToken, Task<object>> next) => next(request, cancellationToken);
    }

    private sealed class NoOpNotificationHandler : INotificationHandler<BenchmarkNotification>
    {
        public Task HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
