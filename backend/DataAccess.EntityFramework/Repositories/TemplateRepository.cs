using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Template;
using TemplateEntity = Business.Models.Entities.Template;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateRepository : Repository<AppDbContext, TemplateEntity, DalEntity>, ITemplateRepository
{
	public TemplateRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
