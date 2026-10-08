using Business.Models.Enums;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using TransactionEntryEntity = Business.Models.Entities.TransactionEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TransactionEntryRepository : Repository<AppDbContext, TransactionEntryEntity, TransactionEntry>, ITransactionEntryRepository
{
	public TransactionEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<TransactionEntryEntity>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
	{
		List<TransactionEntry> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);
		return Mapper.Map<TransactionEntry, TransactionEntryEntity>(entries);
	}

	public Task<bool> HasByAccountId(Guid accountId, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking().AnyAsync(entry => entry.AccountId == accountId, cancellationToken);

	public async Task<ICollection<TransactionEntryEntity>> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default)
	{
		List<TransactionEntry> entries = await Entities.AsNoTracking()
			.Where(entry => entry.TransactionId == transactionId).ToListAsync(cancellationToken);
		return Mapper.Map<TransactionEntry, TransactionEntryEntity>(entries);
	}
	public Task RemoveRange(IEnumerable<TransactionEntryEntity> entries, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Dictionary<Guid, TransactionEntry> tracked = Entities.Local.ToDictionary(entry => entry.Id);
		foreach (TransactionEntryEntity entry in entries)
		{
			Entities.Remove(tracked.TryGetValue(entry.Id, out TransactionEntry? existing)
				? existing : Mapper.Map<TransactionEntry, TransactionEntryEntity>(entry));
		}
		return Task.CompletedTask;
	}

	public Task<decimal?> GetPrevious(Guid accountId, DateTime beforeDateTime, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId && entry.Transaction!.DeleteRevision == null &&
				entry.Transaction.State == TransactionState.Confirmed && entry.Transaction.DateTime < beforeDateTime)
			.OrderByDescending(entry => entry.Transaction!.DateTime)
			.ThenByDescending(entry => entry.TransactionId).ThenByDescending(entry => entry.Position)
			.Select(entry => (decimal?)entry.CumulativeAmount).FirstOrDefaultAsync(cancellationToken);
}
