using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using ProjectEntity = Business.Models.Entities.Project;
using ProjectGroupEntity = Business.Models.Entities.ProjectGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class ProjectRepository : ElementRepository<ProjectGroupEntity, ProjectEntity, Project>, IProjectRepository
{
	public ProjectRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
