using Business.Models.Entities.Interfaces;

namespace DataAccess.Contracts.Repositories.Base;

public interface IElementRepository<TGroup, TElement> : ITrackedRepository<TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>, ITrackedEntity
{
	public Task<ICollection<TElement>> GetByName(string name, CancellationToken cancellationToken = default);
	public Task<TGroup> GetGroupWithElementsByGroupId(Guid groupId, CancellationToken cancellationToken = default);
	public Task<int> GetMaxOrderInGroup(Guid groupId, CancellationToken cancellationToken = default);
	public Task<int> GetCountInGroup(Guid groupId, CancellationToken cancellationToken = default);
}
