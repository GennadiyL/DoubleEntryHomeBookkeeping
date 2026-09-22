using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines the shared persistent state of elements belonging to catalog groups.
/// It extends catalog data with the owning group relationship and foreign key.
/// Derived element models expose a writable identifier inherited from BaseEntity.
/// Identifiers can be assigned when instances are created or materialized.
/// The class provides relationship storage without implementing business workflows.
/// </summary>
public abstract class ElementEntity<TGroup, TElement> : CatalogEntity, IElementEntity<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Group { get; set; } = null!;
    public Guid GroupId { get; set; }
}
