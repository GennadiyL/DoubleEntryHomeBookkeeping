using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines movement and replacement operations for grouped catalog elements.
/// Concrete services supply their own read and editor mutation contracts.
/// Groups and elements belong to matching catalog families.
/// Classification and account replacement have distinct reference-replacement rules.
/// Template element replacement is unsupported.
/// Successful state changes and tracking commit as one local action.
/// </summary>
public interface IElementService<TGroup, TElement> : ICatalogService<TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
{
	/// <summary>
	/// Moves a catalog element to a live group of the same type for catalog maintenance.
	/// Preserves identity and validates the applicable name-uniqueness rules.
	/// Adds Content and Order to the moved element and Order to shifted siblings.
	/// Normalizes the source and destination element sequences and commits the complete action together.
	/// </summary>
	public Task MoveToAnotherGroup(Guid entityId, Guid toGroupId, CancellationToken cancellationToken = default);
	/// <summary>
	/// Replaces source references with the destination for supported catalog merges.
	/// Equal identities return immediately without loading or saving for supported types.
	/// Classification merges replace only the matching account classification and preserve account names.
	/// Account replacement requires the same currency and preserves entry amounts, rates and positions.
	/// Changed accounts or parent aggregates receive Content; the source is soft-deleted.
	/// Deleting the selected balancing account preserves its System setting; configuration reads report no available selection.
	/// All changes commit once or roll back together.
	/// Template merging is unsupported and throws NotSupportedException without changing data.
	/// </summary>
	public Task CombineElements(Guid toElementId, Guid fromElementId, CancellationToken cancellationToken = default);
}
