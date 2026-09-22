using Business.Models.Entities.Interfaces;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.Core.EntityFramework.Behaviors;

namespace DataAccess.EntityFramework.Repositories.Base;

internal abstract class ElementRepository<TGroup, TElement, TD> : Repository<AppDbContext, TElement, TD>, IElementRepository<TGroup, TElement>
	where TGroup : class, IGroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, new()
	where TD : class, IDalEntity, new()
{
	public ElementRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public Task<ICollection<TElement>> GetByName(string name) => throw new NotImplementedException();

	public override void Update(TElement entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		TD? tracked = Entities.Local.FirstOrDefault(item => item.Id == entity.Id);
		if (tracked is null)
		{
			base.Update(entity);
			return;
		}

		Context.Entry(tracked).CurrentValues.SetValues(Mapper.Map<TD, TElement>(entity));
	}

	public Task<TGroup> GetGroupWithElementsByGroupId(Guid groupId) => throw new NotImplementedException();

	public Task<int> GetMaxOrderInGroup(Guid groupId) => throw new NotImplementedException();

	public Task<int> GetCountInGroup(Guid groupId) => throw new NotImplementedException();
}
