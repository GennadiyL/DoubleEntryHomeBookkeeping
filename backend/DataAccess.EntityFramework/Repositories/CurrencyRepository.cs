using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Repositories.Base;
using DataAccess.EntityFramework.Models;
using CurrencyEntity = Business.Models.Entities.Currency;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRepository : TrackedRepository<CurrencyEntity, Currency>, ICurrencyRepository
{
	public CurrencyRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
