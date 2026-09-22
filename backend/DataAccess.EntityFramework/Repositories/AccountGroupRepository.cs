using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.AccountGroup;
using AccountGroupEntity = Business.Models.Entities.AccountGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class AccountGroupRepository : Repository<AppDbContext, AccountGroupEntity, DalEntity>, IAccountGroupRepository
{
	public AccountGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
