namespace Business.Core.Entities;

/// <summary>
/// Defines the persistent business entity base.
/// Supplies Guid identity and identity-based equality for persistent business models.
/// Concrete model classes derive from it and may accept an existing identifier when materialized.
/// It implements IBaseEntity and establishes the common identity semantics used by data mapping.
/// It does not add domain behavior or persistence operations.
/// </summary>
public abstract class BaseEntity : IBaseEntity, IEquatable<IBaseEntity>
{
	public Guid Id { get; set; }

	public bool Equals(IBaseEntity? other) => other != null && Id.Equals(other.Id);

	public override bool Equals(object? obj) => Equals(obj as BaseEntity);

	public override int GetHashCode() => Id.GetHashCode();

	public override string ToString() => $"{GetType().Name} #{Id}";
}
