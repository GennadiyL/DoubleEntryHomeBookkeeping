using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.TransactionEntry;
using TransactionEntryEntity = Business.Models.Entities.TransactionEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TransactionEntryRepository : Repository<AppDbContext, TransactionEntryEntity, DalEntity>, ITransactionEntryRepository
{
	public TransactionEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
