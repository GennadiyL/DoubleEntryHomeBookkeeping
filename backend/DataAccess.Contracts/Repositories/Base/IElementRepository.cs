using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories.Base;

public interface IElementRepository<TGroup, TElement> : IRepository<TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public Task<ICollection<TElement>> GetByName(string name);
	public Task<TGroup> GetGroupWithElementsByGroupId(Guid groupId);
	public Task<int> GetMaxOrderInGroup(Guid groupId);
	public Task<int> GetCountInGroup(Guid groupId);
}
