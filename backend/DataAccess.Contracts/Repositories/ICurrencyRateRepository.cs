using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ICurrencyRateRepository : IRepository<CurrencyRate>
{
	public Task<CurrencyRate?> GetApplicable(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default);
	public Task<CurrencyRate?> GetByCurrencyAndDate(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default);
	public Task<ICollection<CurrencyRate>> GetByCurrencyId(Guid currencyId, CancellationToken cancellationToken = default);
	public Task<ICollection<CurrencyRate>> GetByCurrencyAndDateRange(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
