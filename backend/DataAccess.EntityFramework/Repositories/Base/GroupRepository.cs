using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework;
using Microsoft.EntityFrameworkCore;

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

	public override void Update(TGroup entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		TD? tracked = Entities.Local.FirstOrDefault(item => item.Id == entity.Id);
		if (tracked is null)
		{
			base.Update(entity);
			return;
		}

		Context.Entry(tracked).CurrentValues.SetValues(Mapper.Map<TD, TGroup>(entity));
	}

	public async Task<TGroup?> GetWithChildrenByIdAsync(Guid id)
	{
		TD? group = await Entities.AsNoTracking().Include("Children")
			.SingleOrDefaultAsync(entity => entity.Id == id);
		return group is null ? null : Mapper.Map<TD, TGroup>(group);
	}

	public Task<int> GetMaxOrderInParent(Guid? parentId) => throw new NotImplementedException();

	public async Task<TGroup?> GetWithContentsByIdAsync(Guid id)
	{
		TD? group = await Entities.AsNoTracking().Include("Children").Include("Elements")
			.SingleOrDefaultAsync(entity => entity.Id == id);
		return group is null ? null : Mapper.Map<TD, TGroup>(group);
	}

	public Task<int> GetCountInParent(Guid? parentId) => throw new NotImplementedException();
}
