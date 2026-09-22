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

	public async Task<ICollection<TransactionEntryEntity>> GetByAccountIdAsync(Guid accountId)
	{
		List<DalEntity> entries = await Entities.AsNoTracking()
			.Where(entry => entry.AccountId == accountId).ToListAsync();
		return Mapper.Map<DalEntity, TransactionEntryEntity>(entries);
	}

	public Task<bool> HasByAccountIdAsync(Guid accountId) =>
		Entities.AnyAsync(entry => entry.AccountId == accountId);

	public override void Update(TransactionEntryEntity entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		DalEntity? tracked = Entities.Local.FirstOrDefault(item => item.Id == entity.Id);
		if (tracked is null)
		{
			base.Update(entity);
			return;
		}

		Context.Entry(tracked).CurrentValues.SetValues(Mapper.Map<DalEntity, TransactionEntryEntity>(entity));
	}
}
