using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using LocalConfigEntity = Business.Models.Entities.Config.LocalConfig;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class LocalConfigRepository : Repository<AppDbContext, LocalConfigEntity, LocalConfig>, ILocalConfigRepository
{
	public LocalConfigRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
