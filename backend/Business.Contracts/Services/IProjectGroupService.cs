using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Contracts.Services.Groups;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IProjectGroupService : IGroupService<ProjectGroup, Project>, IUpdateEntityService<GroupParam>
{
}
