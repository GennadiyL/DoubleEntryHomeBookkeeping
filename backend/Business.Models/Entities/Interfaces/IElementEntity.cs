using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Defines membership of an element in its matching catalog group.
/// Group and GroupId identify the same parent group.
/// Direct elements have an ordering sequence separate from child groups.
/// Movement preserves element identity and changes its group membership.
/// </summary>
public interface IElementEntity<TGroup, TElement> : IBaseEntity
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Group { get; set; }
	public Guid GroupId { get; set; }
}
