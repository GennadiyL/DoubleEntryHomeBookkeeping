using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using CurrencyEntity = Business.Models.Entities.Currency;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRepository : Repository<AppDbContext, CurrencyEntity, Currency>, ICurrencyRepository
{
	public CurrencyRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
