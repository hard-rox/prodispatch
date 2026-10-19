using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Dispatcher;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Abstractions.Queries;
using ProDispatch.Abstractions.Requests;
using ProDispatch.DependencyInjection;
using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Scanning_ResolvesRequestAndInheritedCommandContracts()
    {
        ServiceCollection services = new();
        services.AddScoped<ScopedDependency>();
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();
        IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
        ScopedDependency dependency = scope.ServiceProvider.GetRequiredService<ScopedDependency>();

        Assert.Equal(dependency.Id, await dispatcher.Send(new ScopeRequest(), TestContext.Current.CancellationToken));
        Assert.Equal(42, await dispatcher.SendAsync(new ScannedCommand(), TestContext.Current.CancellationToken));
        Assert.IsType<ScannedCommandHandler>(scope.ServiceProvider.GetRequiredService<IRequestHandler<ScannedCommand, int>>());
        Assert.IsType<ScannedCommandHandler>(scope.ServiceProvider.GetRequiredService<ICommandHandler<ScannedCommand, int>>());
        Assert.Equal(UnitType.Value, await dispatcher.Send(new VoidRequest(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Dispatcher_UsesCurrentScopeForHandlerDependencies()
    {
        ServiceCollection services = new();
        services.AddScoped<ScopedDependency>();
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        IDispatcher first = firstScope.ServiceProvider.GetRequiredService<IDispatcher>();
        IDispatcher second = secondScope.ServiceProvider.GetRequiredService<IDispatcher>();

        Guid firstId = await first.Send(new ScopeRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(firstId, await first.Send(new ScopeRequest(), TestContext.Current.CancellationToken));
        Assert.NotEqual(firstId, await second.Send(new ScopeRequest(), TestContext.Current.CancellationToken));
        Assert.NotSame(first, firstScope.ServiceProvider.GetRequiredService<IDispatcher>());
    }

    [Fact]
    public async Task RepeatedRegistration_DoesNotDuplicateNotificationsOrBehaviors()
    {
        List<string> events = [];
        ServiceCollection services = new();
        services.AddSingleton(events);
        for (int i = 0; i < 2; i++)
        {
            services.AddProDispatch(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly);
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly);
                cfg.AddOpenBehavior(typeof(RecordingBehavior<,>));
            });
        }

        using ServiceProvider provider = services.BuildServiceProvider();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        await dispatcher.SendAsync(new ScannedCommand(), TestContext.Current.CancellationToken);
        await dispatcher.PublishAsync(new ScannedNotification(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "open:enter", "open:exit", "notification:first", "notification:second" }, events);
    }

    [Fact]
    public async Task OpenAndClosedBehaviors_PreserveOrderAndRunOnceForUnitCommands()
    {
        List<string> events = [];
        ServiceCollection services = new();
        services.AddSingleton(events);
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly);
            cfg.AddOpenBehavior(typeof(RecordingBehavior<,>));
            cfg.AddBehavior<ClosedVoidBehavior>();
            cfg.AddBehavior<LegacyVoidBehavior>();
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IDispatcher>().SendAsync(new VoidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "open:enter", "closed:enter", "legacy:enter", "handler", "legacy:exit", "closed:exit", "open:exit" }, events);
    }

    [Fact]
    public async Task ScopedBehaviors_ApplyOnlyToMatchingRequests()
    {
        List<string> events = [];
        ServiceCollection services = new();
        services.AddSingleton(events);
        services.AddScoped<ScopedDependency>();
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly);
            cfg.AddOpenBehavior(typeof(CommandBehavior<,>));
            cfg.AddOpenBehavior(typeof(QueryBehavior<,>));
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new ScannedCommand(), TestContext.Current.CancellationToken);
        await dispatcher.SendAsync(new ScannedQuery(), TestContext.Current.CancellationToken);
        await dispatcher.Send(new ScopeRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "command", "query" }, events);
    }

    [Fact]
    public async Task Scanning_PreservesApplicationHandlerRegistration()
    {
        ServiceCollection services = new();
        services.AddTransient<IRequestHandler<ScannedCommand, int>, ZOverrideHandler>();
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal(99, await provider.GetRequiredService<IDispatcher>().SendAsync(new ScannedCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ActivationFailure_IsNotReplacedByMissingHandlerError()
    {
        ServiceCollection services = new();
        services.AddTransient<BrokenDependency>(_ => throw new InvalidOperationException("activation failed"));
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.SendAsync(new BrokenCommand(), TestContext.Current.CancellationToken));

        Assert.Equal("activation failed", error.Message);
    }

    [Fact]
    public async Task MissingHandler_ReportsRequestType()
    {
        ServiceCollection services = new();
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRequiredService<IDispatcher>().Send(new MissingRequest(), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(MissingRequest), error.Message);
    }

    [Fact]
    public async Task SpecificHandlerContracts_RemainUsableWhenRequestContractIsAbsent()
    {
        List<string> events = [];
        ServiceCollection services = new();
        services.AddSingleton(events);
        services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly));
        services.RemoveAll<IRequestHandler<VoidCommand, UnitType>>();
        services.RemoveAll<IRequestHandler<ScannedCommand, int>>();
        services.RemoveAll<IRequestHandler<ScannedQuery, int>>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new VoidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(42, await dispatcher.SendAsync(new ScannedCommand(), TestContext.Current.CancellationToken));
        Assert.Equal(42, await dispatcher.SendAsync(new ScannedQuery(), TestContext.Current.CancellationToken));
        Assert.Equal(new[] { "handler" }, events);
    }

    [Fact]
    public async Task CancellationToken_ReachesScannedHandlerThroughOpenBehavior()
    {
        ServiceCollection services = new();
        services.AddSingleton(new List<string>());
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionTests).Assembly);
            cfg.AddOpenBehavior(typeof(RecordingBehavior<,>));
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        CancellationToken actual = await provider.GetRequiredService<IDispatcher>().Send(new TokenRequest(), source.Token);

        Assert.Equal(source.Token, actual);
    }

    [Fact]
    public void Registration_RejectsMissingAssembliesAndInvalidBehaviors()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentNullException>(() => services.AddProDispatch(null!));
        Assert.Throws<InvalidOperationException>(() => services.AddProDispatch(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ProDispatchServiceConfiguration().RegisterServicesFromAssembly(null!));
        Assert.Throws<ArgumentException>(() => new ProDispatchServiceConfiguration().AddBehavior(typeof(string)));
        Assert.Throws<ArgumentException>(() => new ProDispatchServiceConfiguration().AddBehavior(typeof(RecordingBehavior<,>)));
        Assert.Throws<ArgumentException>(() => new ProDispatchServiceConfiguration().AddOpenBehavior(typeof(ClosedVoidBehavior)));
        Assert.Throws<ArgumentException>(() => new ProDispatchServiceConfiguration().AddOpenBehavior(typeof(ReversedBehavior<,>)));
        Assert.Empty(services);
    }

    private sealed class ScopedDependency
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed record ScopeRequest : IRequest<Guid>;

    private sealed class ScopeRequestHandler(ScopedDependency dependency) : IRequestHandler<ScopeRequest, Guid>
    {
        public Task<Guid> HandleAsync(ScopeRequest request, CancellationToken cancellationToken) => Task.FromResult(dependency.Id);
    }

    private sealed record ScannedCommand : ICommand<int>;

    private sealed class ScannedCommandHandler : ICommandHandler<ScannedCommand, int>
    {
        public Task<int> HandleAsync(ScannedCommand command, CancellationToken cancellationToken) => Task.FromResult(42);
    }

    private sealed class ZOverrideHandler : IRequestHandler<ScannedCommand, int>
    {
        public Task<int> HandleAsync(ScannedCommand request, CancellationToken cancellationToken) => Task.FromResult(99);
    }

    private sealed record VoidRequest : IRequest;

    private sealed class VoidRequestHandler : IRequestHandler<VoidRequest>
    {
        public Task<UnitType> HandleAsync(VoidRequest request, CancellationToken cancellationToken) => Task.FromResult(UnitType.Value);
    }

    private sealed record VoidCommand : ICommand;

    private sealed class VoidCommandHandler(List<string> events) : ICommandHandler<VoidCommand>
    {
        public Task HandleAsync(VoidCommand command, CancellationToken cancellationToken)
        {
            events.Add("handler");
            return Task.CompletedTask;
        }
    }

    private sealed record ScannedQuery : IQuery<int>;

    private sealed class ScannedQueryHandler : IQueryHandler<ScannedQuery, int>
    {
        public Task<int> HandleAsync(ScannedQuery query, CancellationToken cancellationToken) => Task.FromResult(42);
    }

    private sealed record ScannedNotification : INotification;

    private sealed class FirstNotificationHandler(List<string> events) : INotificationHandler<ScannedNotification>
    {
        public Task HandleAsync(ScannedNotification notification, CancellationToken cancellationToken)
        {
            events.Add("notification:first");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondNotificationHandler(List<string> events) : INotificationHandler<ScannedNotification>
    {
        public Task HandleAsync(ScannedNotification notification, CancellationToken cancellationToken)
        {
            events.Add("notification:second");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBehavior<TRequest, TResponse>(List<string> events) : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            events.Add("open:enter");
            TResponse result = await next(request, cancellationToken);
            events.Add("open:exit");
            return result;
        }
    }

    private sealed class ClosedVoidBehavior(List<string> events) : IPipelineBehavior<VoidCommand, UnitType>
    {
        public async Task<UnitType> HandleAsync(VoidCommand request, CancellationToken cancellationToken, Func<VoidCommand, CancellationToken, Task<UnitType>> next)
        {
            events.Add("closed:enter");
            UnitType result = await next(request, cancellationToken);
            events.Add("closed:exit");
            return result;
        }
    }

    private sealed class LegacyVoidBehavior(List<string> events) : IPipelineBehavior<VoidCommand, object>
    {
        public async Task<object> HandleAsync(VoidCommand request, CancellationToken cancellationToken, Func<VoidCommand, CancellationToken, Task<object>> next)
        {
            events.Add("legacy:enter");
            object result = await next(request, cancellationToken);
            events.Add("legacy:exit");
            return result;
        }
    }

    private sealed class CommandBehavior<TRequest, TResponse>(List<string> events) : IPipelineBehavior<TRequest, TResponse>, ICommandPipelineBehavior
    {
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            events.Add("command");
            return next(request, cancellationToken);
        }
    }

    private sealed class QueryBehavior<TRequest, TResponse>(List<string> events) : IPipelineBehavior<TRequest, TResponse>, IQueryPipelineBehavior
    {
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            events.Add("query");
            return next(request, cancellationToken);
        }
    }

    private sealed class ReversedBehavior<TResponse, TRequest> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<TResponse>> next) => next(request, cancellationToken);
    }

    private sealed record BrokenCommand : ICommand<int>;

    private sealed class BrokenDependency;

    private sealed class BrokenCommandHandler(BrokenDependency dependency) : ICommandHandler<BrokenCommand, int>
    {
        public Task<int> HandleAsync(BrokenCommand command, CancellationToken cancellationToken) => Task.FromResult(dependency.GetHashCode());
    }

    private sealed record MissingRequest : IRequest<int>;

    private sealed record TokenRequest : IRequest<CancellationToken>;

    private sealed class TokenRequestHandler : IRequestHandler<TokenRequest, CancellationToken>
    {
        public Task<CancellationToken> HandleAsync(TokenRequest request, CancellationToken cancellationToken) => Task.FromResult(cancellationToken);
    }
}