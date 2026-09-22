using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using CategoryGroupEntity = Business.Models.Entities.CategoryGroup;
using CategoryEntity = Business.Models.Entities.Category;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CategoryGroupRepository : GroupRepository<CategoryGroupEntity, CategoryEntity, CategoryGroup>, ICategoryGroupRepository
{
	public CategoryGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
