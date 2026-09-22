using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Correspondent;
using CorrespondentEntity = Business.Models.Entities.Correspondent;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CorrespondentRepository : Repository<AppDbContext, CorrespondentEntity, DalEntity>, ICorrespondentRepository
{
	public CorrespondentRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
