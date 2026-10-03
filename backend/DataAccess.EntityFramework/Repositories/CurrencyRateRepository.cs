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

	public async Task<CurrencyRateEntity?> GetApplicable(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default)
	{
		DalEntity? rate = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.Date <= date && item.DeleteRevision == null)
			.OrderByDescending(item => item.Date).FirstOrDefaultAsync(cancellationToken);
		return rate is null ? null : Mapper.Map<DalEntity, CurrencyRateEntity>(rate);
	}

	public async Task<CurrencyRateEntity?> GetByCurrencyAndDate(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default)
	{
		DalEntity? rate = await Entities.AsNoTracking()
			.SingleOrDefaultAsync(item => item.CurrencyId == currencyId && item.Date == date, cancellationToken);
		return rate is null ? null : Mapper.Map<DalEntity, CurrencyRateEntity>(rate);
	}

	public async Task<ICollection<CurrencyRateEntity>> GetByCurrencyId(Guid currencyId, CancellationToken cancellationToken = default)
	{
		List<DalEntity> rates = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.DeleteRevision == null).ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, CurrencyRateEntity>(rates);
	}

	public async Task<ICollection<CurrencyRateEntity>> GetByCurrencyAndDateRange(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
	{
		List<DalEntity> rates = await Entities.AsNoTracking()
			.Where(item => item.CurrencyId == currencyId && item.Date >= fromDate && item.Date <= toDate && item.DeleteRevision == null)
			.ToListAsync(cancellationToken);
		return Mapper.Map<DalEntity, CurrencyRateEntity>(rates);
	}
}
