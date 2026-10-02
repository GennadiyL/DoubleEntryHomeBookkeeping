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
}
