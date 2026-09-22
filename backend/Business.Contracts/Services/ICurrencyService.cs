using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICurrencyService : IOrderedService<Currency>, IFavoriteService<Currency>
{
	public Task<Guid> Add(CurrencyParam param, decimal initialRate);
	public Task Update(Guid currencyId, CurrencyParam param);
	public Task Delete(Guid currencyId);
}
