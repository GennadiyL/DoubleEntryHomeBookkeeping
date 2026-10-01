using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Trees;

/// <summary>
/// Supplies editable values for a non-root group in any of the five catalogs.
/// ParentId selects a live parent in the matching group hierarchy.
/// Name is trimmed, nonblank and unique among sibling groups.
/// Description is optional and independent of Name.
/// IsFavorite applies only to this group, without cascading to descendants.
/// The caller does not supply identity, order or synchronization metadata.
/// Services enforce root protection and prevent hierarchy cycles.
/// Add or Update validates and commits the complete action atomically.
/// </summary>
public record GroupParam : INamedParam, IFavoriteParam, IGroupParam
{
	public Guid ParentId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}
