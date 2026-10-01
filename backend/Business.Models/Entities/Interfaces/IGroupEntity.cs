using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Defines same-type parent, child-group and element relationships.
/// The fixed root references itself as parent and is excluded from its own children.
/// Children and Elements contain direct members rather than recursive descendants.
/// Their accepted ordering sequences are separate and zero-based.
/// Services prevent cycles and protect root identities.
/// </summary>
public interface IGroupEntity<TGroup, TElement> : IBaseEntity
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Parent { get; set; }
	public Guid ParentId { get; set; }
	public ICollection<TGroup> Children { get; set; }
	public ICollection<TElement> Elements { get; set; }
}
