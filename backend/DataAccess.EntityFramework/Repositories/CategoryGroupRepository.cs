using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.CategoryGroup;
using CategoryGroupEntity = Business.Models.Entities.CategoryGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CategoryGroupRepository : Repository<AppDbContext, CategoryGroupEntity, DalEntity>, ICategoryGroupRepository
{
	public CategoryGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
