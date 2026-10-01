using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Contracts.Services.Groups;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICategoryGroupService : IGroupService<CategoryGroup, Category>, IUpdateEntityService<GroupParam>
{
}
