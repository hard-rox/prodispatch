using ProDispatch.Abstractions.Requests;

namespace ProDispatch.Abstractions.Queries;

/// <summary>
/// Generic handler interface for queries.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>Handles the specified query and returns a result.</summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The query result.</returns>
    new Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);

    /// <summary>Handles the specified query and returns a result.</summary>
    /// <param name="request">The query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The query result.</returns>
    Task<TResult> IRequestHandler<TQuery, TResult>.HandleAsync(TQuery request, CancellationToken cancellationToken) =>
        HandleAsync(request, cancellationToken);
}
