using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

/// <summary>
/// Persists the Local pending synchronization document as text.
/// Id retains the business entity's fixed singleton identity.
/// Json holds the serialized container without interpreting its content.
/// The shared mapper transfers both scalar properties without conversion.
/// Repository writes use the owning database context and transaction.
/// This metadata has no business change-tracking fields or navigation properties.
/// The synchronization workflow controls retention and snapshot preservation.
/// Serialization and validation remain outside the persistence model.
/// </summary>
internal class LocalJson : IDalEntity
{
	public Guid Id { get; set; }
	public string Json { get; set; } = string.Empty;
}
