using Business.Contracts.Services.Currencies;
using Business.Contracts.Params;
using Business.Models.Entities;
using Business.Contracts.Base.Services;

namespace Business.Contracts.Services;

public interface ICurrencyService : IOrderedService<Currency>, IFavoriteService<Currency>
{
	public Task<Guid> Add(CurrencyParam param, decimal initialRate);
	public Task Update(Guid currencyId, CurrencyParam param);
	public Task Delete(Guid currencyId);
	/// <summary>
	/// Returns the offline culture-based currency catalog for the currency creation selector.
	/// Skips neutral or invalid cultures and keeps the first name and symbol for each ISO code.
	/// </summary>
	public Task<List<AvailableCurrencyInfo>> GetAvailableCurrencies();

	/// <summary>
	/// Returns all saved currencies, including the base currency, ordered by Order for browsing and selection.
	/// This read does not save changes.
	/// </summary>
	public Task<List<CurrencyInfo>> GetAllCurrencies();
}
