using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProDispatch.Abstractions.Commands;
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
    
    private IMediator _mediatrNoPipelineMediator = null!;
    private IMediator _mediatrOnePipelineMediator = null!;
    private IMediator _mediatrMultiplePipelineMediator = null!;
    private MediatRBenchmarkCommand _mediatrCommand = null!;
    private IMediator _mediatrNotificationMediator = null!;
    private MediatRBenchmarkNotification _mediatrNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        _commandNoPipelineDispatcher = BuildCommandDispatcherNoPipeline();
        _commandOnePipelineDispatcher = BuildCommandDispatcherOnePipeline();
        _commandMultiplePipelineDispatcher = BuildCommandDispatcherMultiplePipeline();
        _notificationDispatcher = BuildNotificationDispatcher();
        _command = new();
        _notification = new("bench");
        
        _mediatrNoPipelineMediator = BuildMediatRNoPipeline();
        _mediatrOnePipelineMediator = BuildMediatROnePipeline();
        _mediatrMultiplePipelineMediator = BuildMediatRMultiplePipeline();
        _mediatrNotificationMediator = BuildMediatRNotification();
        _mediatrCommand = new();
        _mediatrNotification = new("bench");
    }

    [Benchmark(Baseline = true)]
    public Task ProDispatch_SendCommandNoPipelineAsync() => _commandNoPipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task ProDispatch_SendCommandOnePipelineAsync() => _commandOnePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task ProDispatch_SendCommandMultiplePipelineAsync() => _commandMultiplePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    public Task ProDispatch_PublishNotificationAsync() => _notificationDispatcher.PublishAsync(_notification);
    
    [Benchmark]
    public Task MediatR_SendCommandNoPipelineAsync() => _mediatrNoPipelineMediator.Send(_mediatrCommand);

    [Benchmark]
    public Task MediatR_SendCommandOnePipelineAsync() => _mediatrOnePipelineMediator.Send(_mediatrCommand);

    [Benchmark]
    public Task MediatR_SendCommandMultiplePipelineAsync() => _mediatrMultiplePipelineMediator.Send(_mediatrCommand);

    [Benchmark]
    public Task MediatR_PublishNotificationAsync() => _mediatrNotificationMediator.Publish(_mediatrNotification);

    private static InProcessDispatcher BuildCommandDispatcherNoPipeline()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        return new(factory);
    }

    private static InProcessDispatcher BuildCommandDispatcherOnePipeline()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        factory.Register(typeof(ProDispatch.Abstractions.Pipeline.IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        return new(factory);
    }

    private static InProcessDispatcher BuildCommandDispatcherMultiplePipeline()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        factory.Register(typeof(ProDispatch.Abstractions.Pipeline.IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        factory.Register(typeof(ProDispatch.Abstractions.Pipeline.IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        return new(factory);
    }

    private static InProcessDispatcher BuildNotificationDispatcher()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        factory.Register<ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        factory.Register<ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>>(() => new NoOpNotificationHandler());
        return new(factory);
    }

    private sealed record BenchmarkCommand : ICommand;

    private sealed record BenchmarkNotification(string Name) : ProDispatch.Abstractions.Notifications.INotification;

    private sealed class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
    {
        public Task HandleAsync(BenchmarkCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpBehavior : ProDispatch.Abstractions.Pipeline.IPipelineBehavior<BenchmarkCommand, object>
    {
        public Task<object> HandleAsync(BenchmarkCommand request, CancellationToken cancellationToken, Func<BenchmarkCommand, CancellationToken, Task<object>> next) => next(request, cancellationToken);
    }

    private sealed class NoOpNotificationHandler : ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>
    {
        public Task HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    
    // MediatR setup methods
    private static IMediator BuildMediatRNoPipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>());
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
    
    private static IMediator BuildMediatROnePipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior>();
        });
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
    
    private static IMediator BuildMediatRMultiplePipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior2>();
        });
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
    
    private static IMediator BuildMediatRNotification()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>());
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
    
    // MediatR types
    private sealed record MediatRBenchmarkCommand : MediatR.IRequest<Unit>;
    
    private sealed record MediatRBenchmarkNotification(string Name) : MediatR.INotification;
    
    private sealed class MediatRBenchmarkCommandHandler : MediatR.IRequestHandler<MediatRBenchmarkCommand, Unit>
    {
        public Task<Unit> Handle(MediatRBenchmarkCommand request, CancellationToken cancellationToken) => Task.FromResult(Unit.Value);
    }
    
    private sealed class MediatRNoOpBehavior : MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>
    {
        public Task<Unit> Handle(MediatRBenchmarkCommand request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken) => next();
    }
    
    private sealed class MediatRNoOpBehavior2 : MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>
    {
        public Task<Unit> Handle(MediatRBenchmarkCommand request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken) => next();
    }
    
    private sealed class MediatRBenchmarkNotificationHandler1 : MediatR.INotificationHandler<MediatRBenchmarkNotification>
    {
        public Task Handle(MediatRBenchmarkNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    private sealed class MediatRBenchmarkNotificationHandler2 : MediatR.INotificationHandler<MediatRBenchmarkNotification>
    {
        public Task Handle(MediatRBenchmarkNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    private sealed class MediatRBenchmarkNotificationHandler3 : MediatR.INotificationHandler<MediatRBenchmarkNotification>
    {
        public Task Handle(MediatRBenchmarkNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
