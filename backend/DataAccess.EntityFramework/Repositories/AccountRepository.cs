using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using AccountGroupEntity = Business.Models.Entities.AccountGroup;
using AccountEntity = Business.Models.Entities.Account;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class AccountRepository : ElementRepository<AccountGroupEntity, AccountEntity, Account>, IAccountRepository
{
	public AccountRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
