namespace Prodispatch.Abstractions.Unit;

/// <summary>
/// Represents a void return type for commands.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();

    public override bool Equals(object? obj) => obj is Unit;

    public override int GetHashCode() => 0;

    public override string ToString() => "()";
}
