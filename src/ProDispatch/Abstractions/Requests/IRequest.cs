using System.Diagnostics.CodeAnalysis;
using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Abstractions.Requests;

/// <summary>
/// Marker interface for requests that do not produce a result.
/// </summary>
public interface IRequest : IRequest<UnitType>
{
}

/// <summary>
/// Marker interface for requests that return a result of type <typeparamref name="TResponse"/>.
/// </summary>
[SuppressMessage("Design", "S2326", Justification = "Type parameter conveys result type to dispatcher")]
public interface IRequest<out TResponse>
{
}
