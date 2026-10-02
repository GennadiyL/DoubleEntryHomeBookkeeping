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

	public async Task<ICollection<TemplateEntryEntity>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, TemplateEntryEntity>(entries);
	}

	public async Task<ICollection<TemplateEntryEntity>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.TemplateId == templateId).ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, TemplateEntryEntity>(entries);
	}

	public void RemoveRange(IEnumerable<TemplateEntryEntity> entries)
	{
		Entities.RemoveRange(entries.Select(entry => Mapper.Map<DalEntity, TemplateEntryEntity>(entry)));
	}
}
