namespace ProDispatch.Examples.Console.Behaviors;

/// <summary>
/// Example behavior that only applies to commands.
/// </summary>
public class CommandOnlyBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    ICommandPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        System.Console.WriteLine($"[COMMAND-ONLY] Processing command: {typeof(TRequest).Name}");
        TResponse response = await next(request, cancellationToken);
        System.Console.WriteLine($"[COMMAND-ONLY] Command {typeof(TRequest).Name} completed");
        return response;
    }
}
