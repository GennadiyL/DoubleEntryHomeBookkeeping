using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using CorrespondentEntity = Business.Models.Entities.Correspondent;
using CorrespondentGroupEntity = Business.Models.Entities.CorrespondentGroup;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CorrespondentRepository : ElementRepository<CorrespondentGroupEntity, CorrespondentEntity, Correspondent>, ICorrespondentRepository
{
	public CorrespondentRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
