using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Transaction;
using TransactionEntity = Business.Models.Entities.Transaction;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TransactionRepository : Repository<AppDbContext, TransactionEntity, DalEntity>, ITransactionRepository
{
	public TransactionRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
