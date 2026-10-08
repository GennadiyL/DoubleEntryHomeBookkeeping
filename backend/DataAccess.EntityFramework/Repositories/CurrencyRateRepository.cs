using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using CurrencyRateEntity = Business.Models.Entities.CurrencyRate;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class CurrencyRateRepository : Repository<AppDbContext, CurrencyRateEntity, CurrencyRate>, ICurrencyRateRepository
{
	public CurrencyRateRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<CurrencyRateEntity?> GetApplicable(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default)
	{
		CurrencyRate? rate = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.Date <= date && item.DeleteRevision == null)
			.OrderByDescending(item => item.Date).FirstOrDefaultAsync(cancellationToken);
		return rate is null ? null : Mapper.Map<CurrencyRate, CurrencyRateEntity>(rate);
	}

	public async Task<CurrencyRateEntity?> GetByCurrencyAndDate(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default)
	{
		CurrencyRate? rate = await Entities.AsNoTracking()
			.SingleOrDefaultAsync(item => item.CurrencyId == currencyId && item.Date == date, cancellationToken);
		return rate is null ? null : Mapper.Map<CurrencyRate, CurrencyRateEntity>(rate);
	}

	public async Task<ICollection<CurrencyRateEntity>> GetByCurrencyId(Guid currencyId, CancellationToken cancellationToken = default)
	{
		List<CurrencyRate> rates = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.DeleteRevision == null).ToListAsync(cancellationToken);
		return Mapper.Map<CurrencyRate, CurrencyRateEntity>(rates);
	}

	public async Task<ICollection<CurrencyRateEntity>> GetByCurrencyAndDateRange(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
	{
		List<CurrencyRate> rates = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.Date >= fromDate && item.Date <= toDate && item.DeleteRevision == null)
			.ToListAsync(cancellationToken);
		return Mapper.Map<CurrencyRate, CurrencyRateEntity>(rates);
	}
}
