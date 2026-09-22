using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using AccountGroupEntity = Business.Models.Entities.AccountGroup;
using AccountEntity = Business.Models.Entities.Account;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class AccountGroupRepository : GroupRepository<AccountGroupEntity, AccountEntity, AccountGroup>, IAccountGroupRepository
{
	public AccountGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
