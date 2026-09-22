using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

public interface IElementEntity<TGroup, TElement> : IBaseEntity
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public TGroup Group { get; set; }
	public Guid GroupId { get; set; }
}
