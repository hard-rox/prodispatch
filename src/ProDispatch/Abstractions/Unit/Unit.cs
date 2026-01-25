namespace ProDispatch.Abstractions.Unit;

/// <summary>
/// Represents a void return type for commands.
/// </summary>
public readonly struct Unit
{
    /// <summary>Singleton unit value.</summary>
    public static readonly Unit Value = new();

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";
}
