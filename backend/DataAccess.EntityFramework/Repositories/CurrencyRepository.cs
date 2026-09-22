using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DalEntity = DataAccess.EntityFramework.Models.Currency;
using CurrencyEntity = Business.Models.Entities.Currency;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRepository : Repository<AppDbContext, CurrencyEntity, DalEntity>, ICurrencyRepository
{
	public CurrencyRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
