using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITransactionEntryRepository : IRepository<TransactionEntry>
{
	public Task<ICollection<TransactionEntry>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
	public Task<bool> HasByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
}

