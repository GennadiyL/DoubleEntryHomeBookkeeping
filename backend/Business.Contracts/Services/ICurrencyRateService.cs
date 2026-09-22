using Business.Contracts.Params;

namespace Business.Contracts.Services;

public interface ICurrencyRateService
{
	public Task<Guid> AddOrUpdate(CurrencyRateParam param);
	public Task Delete(Guid currencyId, DateOnly fromDate, DateOnly toDate);
}
