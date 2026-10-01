using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICategoryService :
	IElementService<CategoryGroup, Category>,
	IUpdateEntityService<ElementParam>,
	IReadEntityService<ElementInfo>
{
}
