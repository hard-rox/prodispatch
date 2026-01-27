using System.Diagnostics.CodeAnalysis;
using ProDispatch.Abstractions.Requests;

namespace ProDispatch.Abstractions.Queries;

/// <summary>
/// Marker interface for queries returning a result of type <typeparamref name="TResult"/>.
/// </summary>
[SuppressMessage("Design", "S2326", Justification = "Type parameter conveys result type to dispatcher")]
public interface IQuery<TResult> : IRequest<TResult>
{
}
