using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Trees;

/// <summary>
/// Supplies editable values for a Category, Correspondent or Project.
/// GroupId selects a live group in the matching classification hierarchy.
/// Name is trimmed, nonblank and unique within the group without regard to case.
/// Description is optional and independent of Name.
/// IsFavorite records an explicit selection without inheritance from the group.
/// The caller does not supply identity, order or synchronization metadata.
/// Classification edits do not automatically rename existing accounts.
/// Add or Update validates and commits the complete action atomically.
/// </summary>
public record ElementParam : INamedParam, IFavoriteParam, IElementParam
{
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}
