using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.ProjectGroup;
using ProjectGroupEntity = Business.Models.Entities.ProjectGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class ProjectGroupRepository : Repository<AppDbContext, ProjectGroupEntity, DalEntity>, IProjectGroupRepository
{
	public ProjectGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
