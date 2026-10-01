using Business.Contracts.Services.Trees;
using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines shared hierarchy operations for all five group families.
/// Supports Account, Category, Correspondent, Project and Template groups.
/// Selection reads use detached flat group and lightweight element records.
/// Each hierarchy contains one protected root with a fixed identity.
/// Child groups and direct elements have separate zero-based order sequences.
/// Moves and merges preserve surviving identities and save tracking with data.
/// </summary>
public interface IGroupService<TGroup, TElement> : ICatalogService<TGroup>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
{
	/// <summary>
	/// Returns all live groups of this catalog for parent and destination selection.
	/// Includes the root exactly once in a flat list with parent identities, labels and ordering values.
	/// Deleted groups are excluded; the detached read does not save changes.
	/// </summary>
	public Task<List<GroupInfo>> GetAllGroups(CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns live groups and lightweight elements for browsing any of the five catalogs.
	/// Includes the root once and its direct elements; ParentId and GroupId connect separate flat lists.
	/// Order controls sibling positions; common columns are Name, Description and IsFavorite.
	/// Account classification details and template entries require separate full edit reads.
	/// The detached result does not save changes.
	/// </summary>
	public Task<TreeInfo> GetTree(CancellationToken cancellationToken = default);

	/// <summary>
	/// Moves a non-root group for hierarchy editing while retaining its identity and subtree.
	/// Requires a live same-type parent and rejects cycles and sibling name collisions.
	/// Adds Content and Order to the moved group while preserving received revisions.
	/// Normalizes both sibling collections and adds Order wherever a numeric position changes.
	/// All affected rows commit together; moving to the current parent makes no changes.
	/// </summary>
	public Task MoveToAnotherParent(Guid groupId, Guid toParentId, CancellationToken cancellationToken = default);
	/// <summary>
	/// Combines a source non-root group into a destination for catalog maintenance.
	/// Equal identities return immediately without loading, validation, changes or a commit.
	/// Moves source child groups and elements, resolving incoming name conflicts with repeated _1 suffixes.
	/// Account element names may remain duplicated; subgroup and element ordering are normalized separately.
	/// The emptied source is soft-deleted and applicable content and order flags are preserved or added.
	/// All moves, renames, ordering changes and source deletion commit once or roll back together.
	/// </summary>
	public Task CombineGroups(Guid toGroupId, Guid fromGroupId, CancellationToken cancellationToken = default);
}
