using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ICurrencyRateRepository : IRepository<CurrencyRate>
{
	public Task<CurrencyRate?> GetApplicableAsync(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default);
	public Task<CurrencyRate?> GetByCurrencyAndDateAsync(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default);
	public Task<ICollection<CurrencyRate>> GetByCurrencyIdAsync(Guid currencyId, CancellationToken cancellationToken = default);
	public Task<ICollection<CurrencyRate>> GetByCurrencyAndDateRangeAsync(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
