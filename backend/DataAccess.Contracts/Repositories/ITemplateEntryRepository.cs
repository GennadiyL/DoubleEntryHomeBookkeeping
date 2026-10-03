using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITemplateEntryRepository : IRepository<TemplateEntry>
{
	public Task<ICollection<TemplateEntry>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
	public Task<ICollection<TemplateEntry>> GetByTemplateId(Guid templateId, CancellationToken cancellationToken = default);
	public void RemoveRange(IEnumerable<TemplateEntry> entries);
}

