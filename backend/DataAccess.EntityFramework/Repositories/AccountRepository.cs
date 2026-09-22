using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Account;
using AccountEntity = Business.Models.Entities.Account;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class AccountRepository : Repository<AppDbContext, AccountEntity, DalEntity>, IAccountRepository
{
	public AccountRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
