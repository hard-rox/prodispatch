using ProDispatch.Abstractions.Pipeline;

namespace ProDispatch.Behaviors;

/// <summary>
/// Pipeline behavior that logs request handling details.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>Logs start/end of the request and invokes the next delegate.</summary>
    /// <param name="request">Request being handled.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="next">Next delegate in the pipeline.</param>
    /// <returns>Response produced by the pipeline.</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        var requestType = request?.GetType().Name ?? "Unknown";
        var startTime = DateTime.UtcNow;

        Console.WriteLine($"[LOG] Starting request: {requestType}");

        try
        {
            var response = await next(request!, cancellationToken);
            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"[LOG] Completed request: {requestType} (Duration: {duration.TotalMilliseconds}ms)");
            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"[LOG] Failed request: {requestType} (Duration: {duration.TotalMilliseconds}ms, Error: {ex.Message})");
            throw;
        }
    }
}
