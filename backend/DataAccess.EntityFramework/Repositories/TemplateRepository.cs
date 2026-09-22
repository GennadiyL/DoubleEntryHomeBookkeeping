using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using TemplateEntity = Business.Models.Entities.Template;
using TemplateGroupEntity = Business.Models.Entities.TemplateGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateRepository : ElementRepository<TemplateGroupEntity, TemplateEntity, Template>, ITemplateRepository
{
	public TemplateRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
