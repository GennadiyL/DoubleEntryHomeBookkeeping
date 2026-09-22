using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.TemplateGroup;
using TemplateGroupEntity = Business.Models.Entities.TemplateGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateGroupRepository : Repository<AppDbContext, TemplateGroupEntity, DalEntity>, ITemplateGroupRepository
{
	public TemplateGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
