namespace Business.Contracts.Services.Trees;

/// <summary>
/// Contains the live groups and lightweight elements of one of the five catalogs.
/// Groups includes the fixed root exactly once, with its self-parent relationship.
/// ParentId connects groups; GroupId connects elements, including direct root elements.
/// Groups and elements remain separate flat collections with stable list instances.
/// Each sibling collection uses Order for placement, with canonical GUID ties.
/// Common visible columns are Name, Description and IsFavorite.
/// Account currency display uses AccountTreeInfo; full editing uses separate read records.
/// Reading or changing this detached result does not persist catalog edits.
/// </summary>
public record TreeInfo
{
	public List<GroupInfo> Groups { get; } = new();
	public List<ElementInfo> Elements { get; } = new();
}
