using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Abstractions.Requests;

/// <summary>
/// Generic handler interface for requests that do not produce a typed result.
/// </summary>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, UnitType> where TRequest : IRequest<UnitType>
{
}

/// <summary>
/// Generic handler interface for requests that return a result.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the specified request.</summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The request result.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
