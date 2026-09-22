using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Category;
using CategoryEntity = Business.Models.Entities.Category;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CategoryRepository : Repository<AppDbContext, CategoryEntity, DalEntity>, ICategoryRepository
{
	public CategoryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
