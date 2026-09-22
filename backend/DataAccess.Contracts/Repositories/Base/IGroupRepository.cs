using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories.Base;

public interface IGroupRepository<TGroup, TElement> : IRepository<TGroup>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public Task<ICollection<TGroup>> GetByName(string name);
	public Task<TGroup> GetParentWithChildrenByParentId(Guid parentId);
	public Task<int> GetMaxOrderInParent(Guid? parentId);
	public Task<int> GetCountInParent(Guid? parentId);
}
