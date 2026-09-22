using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Project;
using ProjectEntity = Business.Models.Entities.Project;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class ProjectRepository : Repository<AppDbContext, ProjectEntity, DalEntity>, IProjectRepository
{
	public ProjectRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
