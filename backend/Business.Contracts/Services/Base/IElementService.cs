using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Services.Base;

public interface IElementService<TGroup, TElement, in TParam> : ICatalogService<TElement, TParam>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
	where TParam : class
{
	public Task MoveToAnotherGroup(Guid entityId, Guid toGroupId);
	public Task CombineElements(Guid toElementId, Guid fromElementId);
}
