using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using Microsoft.EntityFrameworkCore;
using DalEntity = DataAccess.EntityFramework.Models.CurrencyRate;
using CurrencyRateEntity = Business.Models.Entities.CurrencyRate;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRateRepository : Repository<AppDbContext, CurrencyRateEntity, DalEntity>, ICurrencyRateRepository
{
	public CurrencyRateRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<CurrencyRateEntity?> GetApplicableAsync(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default)
	{
		DalEntity? rate = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.Date <= date && item.DeleteRevision == null)
			.OrderByDescending(item => item.Date).FirstOrDefaultAsync(cancellationToken);
		return rate is null ? null : Mapper.Map<DalEntity, CurrencyRateEntity>(rate);
	}
}
