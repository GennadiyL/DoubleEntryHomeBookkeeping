using Business.Contracts.Services.Currencies;

namespace Business.Contracts.Services;

public interface ICurrencyRateService
{
	public Task<Guid> AddOrUpdate(CurrencyRateParam param);

	public Task Delete(Guid currencyId, DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Returns all rates for one currency, including the initial rate, in descending Date order for the rate editor.
	/// The UI hides the initial rate date; this read does not save changes.
	/// </summary>
	public Task<List<CurrencyRateInfo>> GetRates(Guid currencyId);

	/// <summary>
	/// Returns the latest rate on or before the selected device-local date for the account currency.
	/// Used by the entry editor; base currency returns one and no stored entry is changed.
	/// </summary>
	public Task<decimal> GetRate(Guid accountId, DateOnly date);
}
