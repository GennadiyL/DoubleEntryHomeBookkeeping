using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.UserConfig;
using UserConfigEntity = Business.Models.Entities.Config.UserConfig;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class UserConfigRepository : Repository<AppDbContext, UserConfigEntity, DalEntity>, IUserConfigRepository
{
	public UserConfigRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
