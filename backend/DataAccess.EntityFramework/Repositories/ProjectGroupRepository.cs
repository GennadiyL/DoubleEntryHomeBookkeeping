using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using ProjectGroupEntity = Business.Models.Entities.ProjectGroup;
using ProjectEntity = Business.Models.Entities.Project;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class ProjectGroupRepository : GroupRepository<ProjectGroupEntity, ProjectEntity, ProjectGroup>, IProjectGroupRepository
{
	public ProjectGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
