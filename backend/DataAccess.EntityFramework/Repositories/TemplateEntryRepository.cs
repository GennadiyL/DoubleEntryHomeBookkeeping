using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.TemplateEntry;
using TemplateEntryEntity = Business.Models.Entities.TemplateEntry;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class TemplateEntryRepository : Repository<AppDbContext, TemplateEntryEntity, DalEntity>, ITemplateEntryRepository
{
	public TemplateEntryRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
