using Business.Models.Entities;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ICurrencyRateRepository : IRepository<CurrencyRate>
{
	public Task<CurrencyRate?> GetApplicableAsync(Guid currencyId, DateOnly date, CancellationToken cancellationToken = default);
}

