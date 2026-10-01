using Business.Contracts.Services.Currencies;

namespace Business.Contracts.Services;

/// <summary>
/// Provides currency-rate upserts, range deletion and rate lookup for editors.
/// Currency identity and calendar date identify an upsert target.
/// Each currency retains one protected initial fallback rate.
/// Rate dates are DateOnly values without timezone conversion.
/// Entry rate lookup uses the device-local transaction date and never rewrites stored entries.
/// Writes validate configured precision and save tracking with the complete action.
/// </summary>
public interface ICurrencyRateService
{
	/// <summary>
	/// Saves a rate-editor value using CurrencyId and Date to find the target.
	/// Updates the matching pair or creates a new row and returns its identity.
	/// Validates configured rounding and a positive rate; the base-currency rate remains one.
	/// The initial date is fixed; choosing another date targets another pair instead of moving a row.
	/// Rate, description and applicable tracking commit together without changing stored transaction rates.
	/// </summary>
	public Task<Guid> AddOrUpdate(CurrencyRateParam param, CancellationToken cancellationToken = default);

	/// <summary>
	/// Soft-deletes ordinary currency rates in an inclusive calendar-date range for the rate editor.
	/// Always excludes the initial rate, even when the range contains its sentinel date.
	/// A reversed range fails validation before any data or tracking change.
	/// All selected deletions and tracking commit together; stored transaction-entry rates are unchanged.
	/// </summary>
	public Task Delete(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all rates for one currency, including the initial rate, in descending Date order for the rate editor.
	/// The UI hides the initial rate date; this read does not save changes.
	/// </summary>
	public Task<List<CurrencyRateInfo>> GetRates(Guid currencyId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the latest rate on or before the selected device-local date for the account currency.
	/// Used by the entry editor; base currency returns one and no stored entry is changed.
	/// </summary>
	public Task<decimal> GetRate(Guid accountId, DateOnly date, CancellationToken cancellationToken = default);
}
