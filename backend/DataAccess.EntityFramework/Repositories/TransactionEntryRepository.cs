using Business.Models.Enums;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using Microsoft.EntityFrameworkCore;
using DalEntity = DataAccess.EntityFramework.Models.TransactionEntry;
using TransactionEntryEntity = Business.Models.Entities.TransactionEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TransactionEntryRepository : Repository<AppDbContext, TransactionEntryEntity, DalEntity>, ITransactionEntryRepository
{
	public TransactionEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<TransactionEntryEntity>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, TransactionEntryEntity>(entries);
	}

	public Task<bool> HasByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking().AnyAsync(entry => entry.AccountId == accountId, cancellationToken);

	public async Task<ICollection<TransactionEntryEntity>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.TransactionId == transactionId).ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, TransactionEntryEntity>(entries);
	}
	public Task RemoveRangeAsync(IEnumerable<TransactionEntryEntity> entries, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Dictionary<Guid, DalEntity> tracked = Entities.Local.ToDictionary(entry => entry.Id);
		foreach (TransactionEntryEntity entry in entries)
		{
			Entities.Remove(tracked.TryGetValue(entry.Id, out DalEntity? existing)
				? existing : Mapper.Map<DalEntity, TransactionEntryEntity>(entry));
		}
		return Task.CompletedTask;
	}

	public Task<decimal?> GetPreviousAsync(Guid accountId, DateTime beforeDateTime, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId && entry.Transaction!.DeleteRevision == null &&
				entry.Transaction.State == TransactionState.Confirmed && entry.Transaction.DateTime < beforeDateTime)
			.OrderByDescending(entry => entry.Transaction!.DateTime)
			.ThenByDescending(entry => entry.TransactionId).ThenByDescending(entry => entry.Position)
			.Select(entry => (decimal?)entry.CumulativeAmount).FirstOrDefaultAsync(cancellationToken);
}
