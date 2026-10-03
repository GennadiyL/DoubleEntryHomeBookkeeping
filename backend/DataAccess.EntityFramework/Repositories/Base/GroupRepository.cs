using Business.Models.Entities.Interfaces;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.Core.EntityFramework.Behaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework.Repositories.Base;

internal abstract class GroupRepository<TGroup, TElement, TD> : Repository<AppDbContext, TGroup, TD>, IGroupRepository<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, new()
	where TD : class, IDalEntity, new()
{
	public GroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public Task<ICollection<TGroup>> GetByName(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public async Task<TGroup?> GetWithChildrenById(Guid id, CancellationToken cancellationToken = default)
	{
		TD? group = await Entities.AsNoTracking().Include("Children")
			.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
		return group is null ? null : Mapper.Map<TD, TGroup>(group);
	}

	public Task<int> GetMaxOrderInParent(Guid? parentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public async Task<TGroup?> GetWithContentsById(Guid id, CancellationToken cancellationToken = default)
	{
		TD? group = await Entities.AsNoTracking().Include("Children").Include("Elements")
			.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
		return group is null ? null : Mapper.Map<TD, TGroup>(group);
	}

	public Task<int> GetCountInParent(Guid? parentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
