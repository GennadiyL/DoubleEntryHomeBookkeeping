using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using TemplateEntryEntity = Business.Models.Entities.TemplateEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateEntryRepository : Repository<AppDbContext, TemplateEntryEntity, TemplateEntry>, ITemplateEntryRepository
{
	public TemplateEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<TemplateEntryEntity>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
	{
		List<TemplateEntry> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);
		return Mapper.Map<TemplateEntry, TemplateEntryEntity>(entries);
	}

	public async Task<ICollection<TemplateEntryEntity>> GetByTemplateId(Guid templateId, CancellationToken cancellationToken = default)
	{
		List<TemplateEntry> entries = await Entities.AsNoTracking()
			.Where(entry => entry.TemplateId == templateId).ToListAsync(cancellationToken);
		return Mapper.Map<TemplateEntry, TemplateEntryEntity>(entries);
	}

	public void RemoveRange(IEnumerable<TemplateEntryEntity> entries)
	{
		Entities.RemoveRange(entries.Select(entry => Mapper.Map<TemplateEntry, TemplateEntryEntity>(entry)));
	}
}
