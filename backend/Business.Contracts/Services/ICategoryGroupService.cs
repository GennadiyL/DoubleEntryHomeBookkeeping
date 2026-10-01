using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICategoryGroupService :
	IGroupService<CategoryGroup, Category>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
}
