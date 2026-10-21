using Business.Models.Enums;
using DataAccess.Contracts.Queries;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Repositories.Base;
using DataAccess.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using TransactionEntity = Business.Models.Entities.Transaction;

namespace DataAccess.EntityFramework.Repositories;

/// <summary>
/// Applies service-supplied transaction criteria in the database.
/// Searches filter parent records before loading complete entries.
/// A transaction matching several entries appears only once.
/// Account and currency references support detached read projection.
/// Database identity ordering follows the configured provider mapping.
/// Updates use the base repository behavior for detached parent values.
/// Entry mutations remain owned by the transaction entry repository.
/// Transaction ownership remains with the application service.
/// </summary>
internal sealed class TransactionRepository : TrackedRepository<TransactionEntity, Transaction>, ITransactionRepository
{
	public TransactionRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<TransactionEntity>> Search(TransactionSearch search, CancellationToken cancellationToken = default)
	{
		IQueryable<Transaction> query = Entities.AsNoTracking().Where(transaction => transaction.DeleteRevision == null);
		if (search.FromDateTime.HasValue)
		{
			query = query.Where(transaction => transaction.DateTime >= search.FromDateTime.Value);
		}
		if (search.BeforeDateTime.HasValue)
		{
			query = query.Where(transaction => transaction.DateTime < search.BeforeDateTime.Value);
		}
		if (search.Ids is not null)
		{
			query = query.Where(transaction => search.Ids.Contains(transaction.Id));
		}
		if (search.State.HasValue)
		{
			query = query.Where(transaction => transaction.State == search.State.Value);
		}
		if (search.AccountId.HasValue)
		{
			query = query.Where(transaction => transaction.Entries.Any(entry => entry.AccountId == search.AccountId.Value));
		}
		if (search.CategoryId.HasValue)
		{
			query = query.Where(transaction => transaction.Entries.Any(entry => entry.Account!.CategoryId == search.CategoryId.Value));
		}
		if (search.CorrespondentId.HasValue)
		{
			query = query.Where(transaction => transaction.Entries.Any(entry => entry.Account!.CorrespondentId == search.CorrespondentId.Value));
		}
		if (search.ProjectId.HasValue)
		{
			query = query.Where(transaction => transaction.Entries.Any(entry => entry.Account!.ProjectId == search.ProjectId.Value));
		}
		query = query.OrderByDescending(transaction => transaction.DateTime).ThenByDescending(transaction => transaction.Id);
		if (search.MaximumCount.HasValue)
		{
			query = query.Take(search.MaximumCount.Value);
		}
		List<Transaction> transactions = await query
			.Include(transaction => transaction.Entries.OrderBy(entry => entry.Position))
			.ThenInclude(entry => entry.Account).ThenInclude(account => account!.Currency).ToListAsync(cancellationToken);
		return Mapper.Map<Transaction, TransactionEntity>(transactions);
	}

	public override async Task<ICollection<TransactionEntity>> GetChanges(CancellationToken cancellationToken = default)
	{
		List<Transaction> entities = await Changes
			.Include(entity => entity.Entries.OrderBy(entry => entry.Position))
			.ToListAsync(cancellationToken);
		return Mapper.Map<Transaction, TransactionEntity>(entities);
	}
}
