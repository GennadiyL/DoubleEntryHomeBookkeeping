using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITemplateEntryRepository : IRepository<TemplateEntry>
{
	public Task<ICollection<TemplateEntry>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
	public Task<ICollection<TemplateEntry>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
	public void RemoveRange(IEnumerable<TemplateEntry> entries);
}

