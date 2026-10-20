namespace DotnetCqrs.Domain.Common;

/// <summary>
/// Classe base para entidades com identidade fortemente tipada.
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is not null && left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);
}
