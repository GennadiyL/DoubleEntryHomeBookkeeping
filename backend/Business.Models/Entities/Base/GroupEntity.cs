using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines shared persistent relationships for hierarchical catalog groups.
/// Stores parent identity and reference within the same group family.
/// Maintains separate direct-child and element collections; the self-parent root excludes itself from children.
/// Services enforce root protection, prevent cycles, and manage hierarchy operations.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
