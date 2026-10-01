using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IProjectService : IElementService<ProjectGroup, Project>, IUpdateEntityService<ElementParam>
{
}
