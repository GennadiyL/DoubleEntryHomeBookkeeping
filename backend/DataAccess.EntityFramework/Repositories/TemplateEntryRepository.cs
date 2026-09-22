using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using Microsoft.EntityFrameworkCore;
using DalEntity = DataAccess.EntityFramework.Models.TemplateEntry;
using TemplateEntryEntity = Business.Models.Entities.TemplateEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateEntryRepository : Repository<AppDbContext, TemplateEntryEntity, DalEntity>, ITemplateEntryRepository
{
	public TemplateEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<TemplateEntryEntity>> GetByAccountIdAsync(Guid accountId)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync();
		return Mapper.Map<DalEntity, TemplateEntryEntity>(entries);
	}

	public override void Update(TemplateEntryEntity entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		DalEntity? tracked = Entities.Local.FirstOrDefault(item => item.Id == entity.Id);
		if (tracked is null)
		{
			base.Update(entity);
			return;
		}

		Context.Entry(tracked).CurrentValues.SetValues(Mapper.Map<DalEntity, TemplateEntryEntity>(entity));
	}
}
