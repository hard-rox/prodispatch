using System.Diagnostics.CodeAnalysis;

namespace ProDispatch.Abstractions.Dispatcher;

/// <summary>
/// Legacy MediatR-compatible dispatcher interface for migration purposes.
/// 
/// Use <see cref="IDispatcher"/> instead. IMediator will be removed in a future version.
/// This interface provides backward compatibility for projects migrating from MediatR.
/// </summary>
[Obsolete("Use IDispatcher instead. IMediator will be removed in a future version.", false)]
[SuppressMessage("Major Code Smell", "S1133:Deprecated code should be removed", Justification = "Obsolete attribute intentionally used for migration path")]
public interface IMediator : IDispatcher
{
}
