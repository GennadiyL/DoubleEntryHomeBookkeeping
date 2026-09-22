using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class ProjectGroupService : GroupService<ProjectGroup, Project>, IProjectGroupService
{
	public ProjectGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.ProjectGroupRepo, unitOfWork.ProjectRepo)
	{
	}
}
