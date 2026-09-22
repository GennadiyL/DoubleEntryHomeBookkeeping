using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using CategoryEntity = Business.Models.Entities.Category;	
using CategoryGroupEntity = Business.Models.Entities.CategoryGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CategoryRepository : ElementRepository<CategoryGroupEntity, CategoryEntity, Category>, ICategoryRepository
{
	public CategoryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
