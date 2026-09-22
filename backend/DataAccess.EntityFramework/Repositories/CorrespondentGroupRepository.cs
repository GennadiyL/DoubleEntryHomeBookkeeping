using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.CorrespondentGroup;
using CorrespondentGroupEntity = Business.Models.Entities.CorrespondentGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CorrespondentGroupRepository : Repository<AppDbContext, CorrespondentGroupEntity, DalEntity>, ICorrespondentGroupRepository
{
	public CorrespondentGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
