namespace Prodispatch.Abstractions.Pipeline;

/// <summary>
/// Pipeline behavior for intercepting request handling.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next);
}
