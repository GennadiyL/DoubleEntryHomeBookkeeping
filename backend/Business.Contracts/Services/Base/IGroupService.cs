using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Services.Base;

public interface IGroupService<TGroup, TElement, in TParam> : ICatalogService<TGroup, TParam>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
	where TParam : class
{
	public Task MoveToAnotherParent(Guid groupId, Guid toParentId);
	public Task CombineGroups(Guid toGroupId, Guid fromGroupId);
}
