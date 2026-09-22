using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITemplateEntryRepository : IRepository<TemplateEntry>
{
	public Task<ICollection<TemplateEntry>> GetByAccountIdAsync(Guid accountId);
}

