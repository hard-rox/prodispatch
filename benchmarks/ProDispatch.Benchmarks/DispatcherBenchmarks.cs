using MediatR;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;
using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.DependencyInjection;
using ProDispatcher = ProDispatch.Abstractions.Dispatcher.IDispatcher;

namespace ProDispatch.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DispatcherBenchmarks
{
    private readonly List<ServiceProvider> _providers = [];
    private ProDispatcher _commandNoPipelineDispatcher = null!;
    private ProDispatcher _commandOnePipelineDispatcher = null!;
    private ProDispatcher _commandMultiplePipelineDispatcher = null!;
    private BenchmarkCommand _command = null!;
    private ProDispatcher _notificationDispatcher = null!;
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
        _commandNoPipelineDispatcher = BuildProDispatch();
        _commandOnePipelineDispatcher = BuildProDispatch(cfg => cfg.AddBehavior<NoOpBehavior>());
        _commandMultiplePipelineDispatcher = BuildProDispatch(cfg =>
        {
            cfg.AddBehavior<NoOpBehavior>();
            cfg.AddBehavior<NoOpBehavior2>();
        });
        _notificationDispatcher = BuildProDispatch();
        _command = new();
        _notification = new("bench");

        _mediatrNoPipelineMediator = BuildMediatRNoPipeline();
        _mediatrOnePipelineMediator = BuildMediatROnePipeline();
        _mediatrMultiplePipelineMediator = BuildMediatRMultiplePipeline();
        _mediatrNotificationMediator = BuildMediatRNotification();
        _mediatrCommand = new();
        _mediatrNotification = new("bench");
    }

    [Benchmark]
    [BenchmarkCategory("SendCommandNoPipeline")]
    public Task ProDispatch_SendCommandNoPipelineAsync() => _commandNoPipelineDispatcher.SendAsync(_command);

    [Benchmark]
    [BenchmarkCategory("SendCommandOnePipeline")]
    public Task ProDispatch_SendCommandOnePipelineAsync() => _commandOnePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    [BenchmarkCategory("SendCommandMultiplePipeline")]
    public Task ProDispatch_SendCommandMultiplePipelineAsync() => _commandMultiplePipelineDispatcher.SendAsync(_command);

    [Benchmark]
    [BenchmarkCategory("PublishNotification")]
    public Task ProDispatch_PublishNotificationAsync() => _notificationDispatcher.PublishAsync(_notification);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendCommandNoPipeline")]
    public Task MediatR_SendCommandNoPipelineAsync() => _mediatrNoPipelineMediator.Send(_mediatrCommand);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendCommandOnePipeline")]
    public Task MediatR_SendCommandOnePipelineAsync() => _mediatrOnePipelineMediator.Send(_mediatrCommand);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SendCommandMultiplePipeline")]
    public Task MediatR_SendCommandMultiplePipelineAsync() => _mediatrMultiplePipelineMediator.Send(_mediatrCommand);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PublishNotification")]
    public Task MediatR_PublishNotificationAsync() => _mediatrNotificationMediator.Publish(_mediatrNotification);

    private ProDispatcher BuildProDispatch(Action<ProDispatchServiceConfiguration>? configure = null)
    {
        ServiceCollection services = new();
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DispatcherBenchmarks).Assembly);
            configure?.Invoke(cfg);
        });
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<ProDispatcher>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (ServiceProvider provider in _providers)
        {
            provider.Dispose();
        }

        _providers.Clear();
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

    private sealed class NoOpBehavior2 : ProDispatch.Abstractions.Pipeline.IPipelineBehavior<BenchmarkCommand, object>
    {
        public Task<object> HandleAsync(BenchmarkCommand request, CancellationToken cancellationToken, Func<BenchmarkCommand, CancellationToken, Task<object>> next) => next(request, cancellationToken);
    }

    private sealed class NoOpNotificationHandler : ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>
    {
        public Task HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpNotificationHandler2 : ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>
    {
        public Task HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpNotificationHandler3 : ProDispatch.Abstractions.Notifications.INotificationHandler<BenchmarkNotification>
    {
        public Task HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // MediatR setup methods
    private IMediator BuildMediatRNoPipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>());
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<IMediator>();
    }

    private IMediator BuildMediatROnePipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior>();
        });
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<IMediator>();
    }

    private IMediator BuildMediatRMultiplePipeline()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRBenchmarkCommand, Unit>, MediatRNoOpBehavior2>();
        });
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<IMediator>();
    }

    private IMediator BuildMediatRNotification()
    {
        ServiceCollection services = new();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DispatcherBenchmarks>());
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
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