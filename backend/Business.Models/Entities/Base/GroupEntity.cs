using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines the shared persistent state of hierarchical catalog groups.
/// It owns parent and child relationships together with the grouped elements.
/// Derived group models expose a writable identifier inherited from BaseEntity.
/// Identifiers can be assigned when instances are created or materialized.
/// The class provides relationship storage without implementing business workflows.
/// </summary>
public abstract class GroupEntity<TGroup, TElement> : CatalogEntity, IGroupEntity<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Parent { get; set; } = null!;
	public Guid ParentId { get; set; }
	public ICollection<TGroup> Children { get; set; } = new List<TGroup>();
	public ICollection<TElement> Elements { get; set; } = new List<TElement>();
}
