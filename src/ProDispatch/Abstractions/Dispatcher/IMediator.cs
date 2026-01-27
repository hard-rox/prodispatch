namespace ProDispatch.Abstractions.Dispatcher;

/// <summary>
/// Legacy MediatR-compatible dispatcher interface for migration purposes.
/// 
/// Use <see cref="IDispatcher"/> instead. IMediator will be removed in a future version.
/// This interface provides backward compatibility for projects migrating from MediatR.
/// </summary>
[Obsolete("Use IDispatcher instead. IMediator will be removed in a future version.", false)]
public interface IMediator : IDispatcher
{
}
