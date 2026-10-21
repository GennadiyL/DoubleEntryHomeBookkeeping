using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Repositories.Base;
using DataAccess.EntityFramework.Models;
using SystemConfigEntity = Business.Models.Entities.Config.SystemConfig;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class SystemConfigRepository : TrackedRepository<SystemConfigEntity, SystemConfig>, ISystemConfigRepository
{
	public SystemConfigRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
