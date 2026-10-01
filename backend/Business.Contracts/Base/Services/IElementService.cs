using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

public interface IElementService<TGroup, TElement> : ICatalogService<TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
{
	public Task MoveToAnotherGroup(Guid entityId, Guid toGroupId);
	public Task CombineElements(Guid toElementId, Guid fromElementId);
}
