using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IProjectGroupService :
	IGroupService<ProjectGroup, Project>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
}
