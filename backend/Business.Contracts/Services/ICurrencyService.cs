using Business.Contracts.Services.Currencies;
using Business.Models.Entities;
using Business.Contracts.Base.Services;

namespace Business.Contracts.Services;

/// <summary>
/// Provides saved currency maintenance and the offline available-currency catalog.
/// Creation stores the currency and its initial fallback rate together.
/// ISO code is immutable and unique; base currency and referenced currencies are protected.
/// Favorites synchronize as content, while manual order uses separate order flags.
/// Reads return detached display records and do not save changes.
/// Each state-changing maintenance action commits with its tracking atomically.
/// </summary>
public interface ICurrencyService :
	IOrderedService<Currency>,
	IFavoriteService<Currency>,
	IReadEntityService<CurrencyInfo>
{
	/// <summary>
	/// Creates a currency from catalog metadata, with Name equal to Code, and its initial fallback rate.
	/// Rejects duplicate ISO codes and validates the initial rate using configured precision.
	/// Initializes new tracked rows with null revisions and None modification flags.
	/// Currency, initial rate and tracking commit together, and the new currency identity is returned.
	/// </summary>
	public Task<Guid> Add(string code, decimal initialRate, CancellationToken cancellationToken = default);
	/// <summary>
	/// Saves a trimmed Name of one to six characters, preserving Code, Symbol and EnglishName.
	/// Unknown or deleted targets throw not-found without saving changes.
	/// Adds Content while preserving existing Order flags and the received edit revision.
	/// Metadata and tracking commit together; stored transaction rates remain unchanged.
	/// </summary>
	public Task Update(Guid currencyId, CurrencyParam param, CancellationToken cancellationToken = default);
	/// <summary>
	/// Soft-deletes an unused non-base currency for currency maintenance.
	/// Unknown or deleted targets throw not-found; base or referenced currencies cannot be deleted.
	/// Preserves EditRevision and modification flags and sets DeleteRevision to zero.
	/// Normalizes surviving currency positions and adds Order to shifted currencies.
	/// The complete action commits atomically; physical cleanup follows delta preparation rules.
	/// </summary>
	public Task Delete(Guid currencyId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the offline culture-based currency catalog for the currency creation selector.
	/// Skips neutral or invalid cultures and keeps the first name and symbol for each ISO code.
	/// </summary>
	public Task<List<AvailableCurrencyInfo>> GetAvailableCurrencies(CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all saved currencies, including the base currency, ordered by Order for browsing and selection.
	/// This read does not save changes.
	/// </summary>
	public Task<List<CurrencyInfo>> GetAllCurrencies(CancellationToken cancellationToken = default);
}
