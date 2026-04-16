namespace ProDispatch.Examples.Console.Behaviors;

/// <summary>
/// Example behavior that only applies to queries.
/// </summary>
public class QueryOnlyBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    IQueryPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        System.Console.WriteLine($"[QUERY-ONLY] Processing query: {typeof(TRequest).Name}");
        TResponse response = await next(request, cancellationToken);
        System.Console.WriteLine($"[QUERY-ONLY] Query {typeof(TRequest).Name} completed");
        return response;
    }
}
