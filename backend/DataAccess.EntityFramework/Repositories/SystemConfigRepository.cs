using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using SystemConfigEntity = Business.Models.Entities.Config.SystemConfig;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class SystemConfigRepository : Repository<AppDbContext, SystemConfigEntity, SystemConfig>, ISystemConfigRepository
{
	public SystemConfigRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
