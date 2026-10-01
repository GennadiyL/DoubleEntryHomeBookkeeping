using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories.Base;

public interface IGroupRepository<TGroup, TElement> : IRepository<TGroup>
	where TGroup : class, IGroupEntity<TGroup, TElement>
	where TElement : class, IElementEntity<TGroup, TElement>
{
	public Task<ICollection<TGroup>> GetByName(string name, CancellationToken cancellationToken = default);
	public Task<TGroup?> GetWithChildrenByIdAsync(Guid id, CancellationToken cancellationToken = default);
	public Task<TGroup?> GetWithContentsByIdAsync(Guid id, CancellationToken cancellationToken = default);
	public Task<int> GetMaxOrderInParent(Guid? parentId, CancellationToken cancellationToken = default);
	public Task<int> GetCountInParent(Guid? parentId, CancellationToken cancellationToken = default);
}
