using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework;

namespace DataAccess.Contracts.Repositories.Base;

internal abstract class GroupRepository<TGroup, TElement, TD> : Repository<AppDbContext, TGroup, TD>, IGroupRepository<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, new()
	where TD : class, IDalEntity, new()
{
	public GroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public Task<ICollection<TGroup>> GetByName(string name) => throw new NotImplementedException();

	public Task<TGroup> GetParentWithChildrenByParentId(Guid parentId) => throw new NotImplementedException();

	public Task<int> GetMaxOrderInParent(Guid? parentId) => throw new NotImplementedException();

	public Task<int> GetCountInParent(Guid? parentId) => throw new NotImplementedException();
}
