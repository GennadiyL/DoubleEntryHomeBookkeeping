using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITransactionEntryRepository : IRepository<TransactionEntry>
{
	public Task<ICollection<TransactionEntry>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
	public Task<bool> HasByAccountId(Guid accountId, CancellationToken cancellationToken = default);
	public Task<ICollection<TransactionEntry>> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default);
	public Task<decimal?> GetPrevious(Guid accountId, DateTime beforeDateTime, CancellationToken cancellationToken = default);
	public Task RemoveRange(IEnumerable<TransactionEntry> entries, CancellationToken cancellationToken = default);
}

