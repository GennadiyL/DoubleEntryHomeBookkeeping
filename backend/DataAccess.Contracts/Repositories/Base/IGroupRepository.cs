using Business.Models.Entities.Interfaces;

namespace DataAccess.Contracts.Repositories.Base;

public interface IGroupRepository<TGroup, TElement> : ITrackedRepository<TGroup>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ITrackedEntity
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public Task<ICollection<TGroup>> GetByName(string name, CancellationToken cancellationToken = default);
	public Task<TGroup?> GetWithChildrenById(Guid id, CancellationToken cancellationToken = default);
	public Task<TGroup?> GetWithContentsById(Guid id, CancellationToken cancellationToken = default);
	public Task<int> GetMaxOrderInParent(Guid? parentId, CancellationToken cancellationToken = default);
	public Task<int> GetCountInParent(Guid? parentId, CancellationToken cancellationToken = default);
}
