using BenchmarkDotNet.Attributes;
using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Dispatcher;
using ProDispatch.ServiceFactory;

namespace ProDispatch.Benchmarks;

[MemoryDiagnoser]
public class DispatcherBenchmarks
{
    private InProcessDispatcher _dispatcher = null!;
    private BenchmarkCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<BenchmarkCommand>>(() => new BenchmarkCommandHandler());
        factory.Register(typeof(IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());
        factory.Register(typeof(IPipelineBehavior<BenchmarkCommand, object>), () => new NoOpBehavior());

        _dispatcher = new InProcessDispatcher(factory);
        _command = new BenchmarkCommand();
    }

    [Benchmark]
    public Task SendCommandAsync() => _dispatcher.SendAsync(_command);

    private sealed record BenchmarkCommand : ICommand;

    private sealed class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
    {
        public Task HandleAsync(BenchmarkCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpBehavior : IPipelineBehavior<BenchmarkCommand, object>
    {
        public async Task<object> HandleAsync(BenchmarkCommand request, CancellationToken cancellationToken, Func<BenchmarkCommand, CancellationToken, Task<object>> next)
        {
            return await next(request, cancellationToken);
        }
    }
}
