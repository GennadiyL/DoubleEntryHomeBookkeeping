using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines the shared relationship between an element and its catalog group.
/// The group reference and foreign key identify the same owning group.
/// Generic types preserve the relationship between a group family and its elements.
/// Services move elements between compatible groups while preserving identity.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public abstract class ElementEntity<TGroup, TElement> : CatalogEntity, IElementEntity<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Group { get; set; } = null!;
	public Guid GroupId { get; set; }
}
