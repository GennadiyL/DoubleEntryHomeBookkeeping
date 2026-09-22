using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

public interface IGroupEntity<TGroup, TElement> : IBaseEntity
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Parent { get; set; }
	public Guid ParentId { get; set; }
	public ICollection<TGroup> Children { get; set; }
	public ICollection<TElement> Elements { get; set; }
}
