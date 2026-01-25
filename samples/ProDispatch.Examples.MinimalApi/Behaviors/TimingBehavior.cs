namespace ProDispatch.Examples.MinimalApi.Behaviors;

using System.Diagnostics;

/// <summary>
/// Pipeline behavior that measures request execution duration.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Executes the timing behavior, measuring how long the handler and subsequent behaviors take.
    /// </summary>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await next(request, cancellationToken);
            stopwatch.Stop();
            Console.WriteLine($"[TIMING] {typeof(TRequest).Name} completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch
        {
            stopwatch.Stop();
            Console.WriteLine($"[TIMING] {typeof(TRequest).Name} failed after {stopwatch.ElapsedMilliseconds}ms");
            throw;
        }
    }
}
