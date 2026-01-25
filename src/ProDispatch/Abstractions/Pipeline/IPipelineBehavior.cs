namespace ProDispatch.Abstractions.Pipeline;

/// <summary>
/// Pipeline behavior for intercepting request handling.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles a request by invoking the next delegate or short-circuiting the pipeline.
    /// </summary>
    /// <param name="request">Request instance.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="next">Delegate to invoke the next behavior/handler.</param>
    /// <returns>Response produced by the pipeline.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next);
}
