using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines common persistent state for named catalog groups and elements.
/// Stores a name, optional description, favorite flag, and catalog order.
/// Edit and delete revisions describe accepted content and deletion state.
/// Modification flags distinguish uncaptured content changes from ordering changes.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
