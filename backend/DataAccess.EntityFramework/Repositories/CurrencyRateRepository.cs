using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.CurrencyRate;
using CurrencyRateEntity = Business.Models.Entities.CurrencyRate;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRateRepository : Repository<AppDbContext, CurrencyRateEntity, DalEntity>, ICurrencyRateRepository
{
	public CurrencyRateRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
