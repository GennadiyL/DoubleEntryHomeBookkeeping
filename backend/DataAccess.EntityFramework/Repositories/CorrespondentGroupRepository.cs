using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using CorrespondentGroupEntity = Business.Models.Entities.CorrespondentGroup;
using CorrespondentEntity = Business.Models.Entities.Correspondent;	

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CorrespondentGroupRepository : GroupRepository<CorrespondentGroupEntity, CorrespondentEntity, CorrespondentGroup>, ICorrespondentGroupRepository
{
	public CorrespondentGroupRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
