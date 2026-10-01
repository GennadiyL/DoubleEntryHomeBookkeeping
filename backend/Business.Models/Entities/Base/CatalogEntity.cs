using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines common persistent state for named catalog groups and elements.
/// Stores a name, optional description, favorite flag and zero-based catalog order.
/// Child groups and elements use separate order sequences within their parent.
/// Creation initializes both revisions to null and modification flags to None.
/// Subsequent content edits and reorders accumulate their applicable modification flags.
/// Application deletion sets DeleteRevision to zero and preserves EditRevision.
/// Identity remains stable through editing, moving and synchronization.
/// Services validate and persist changes; this base model only carries data.
/// </summary>
public abstract class CatalogEntity : BaseEntity, ICatalogEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
}
