using System.Diagnostics.CodeAnalysis;
using ProDispatch.Abstractions.Requests;
using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Abstractions.Commands;

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface ICommand : IRequest<UnitType>
{
}

/// <summary>
/// Marker interface for commands that return a result of type <typeparamref name="TResult"/>.
/// </summary>
[SuppressMessage("Design", "S2326", Justification = "Type parameter conveys result type to dispatcher")]
public interface ICommand<out TResult> : IRequest<TResult>
{
}
