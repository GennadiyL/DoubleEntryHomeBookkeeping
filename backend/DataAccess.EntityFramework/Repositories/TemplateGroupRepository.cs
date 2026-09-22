using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using TemplateGroupEntity = Business.Models.Entities.TemplateGroup;
using TemplateEntity = Business.Models.Entities.Template;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateGroupRepository : GroupRepository<TemplateGroupEntity, TemplateEntity, TemplateGroup>, ITemplateGroupRepository
{
	public TemplateGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
