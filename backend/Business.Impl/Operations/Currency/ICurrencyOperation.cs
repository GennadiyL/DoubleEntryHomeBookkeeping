namespace Business.Impl.Operations.Currency;

/// <summary>
/// Supplies offline currency metadata from installed regional cultures.
/// Returns one profile per ISO currency code.
/// Keeps the first usable regional name and symbol for each code.
/// Skips neutral cultures and cultures without a valid region.
/// Resolves trimmed ISO codes without case sensitivity.
/// Supports throwing and nonthrowing lookup variants.
/// Does not access persisted currencies or exchange rates.
/// The currency service uses these profiles for creation and selection.
/// </summary>
internal interface ICurrencyOperation
{
	/// <summary>
	/// Returns distinct currency profiles for the currency creation selector.
	/// Retains the first usable regional name and symbol for each ISO code.
	/// </summary>
	public List<CurrencyProfile> GetListOfAvailableCurrencyData();

	/// <summary>
	/// Resolves the regional defaults for a trimmed, case-insensitive ISO code.
	/// Throws a descriptive invalid-code exception when the code is blank or unavailable.
	/// </summary>
	public CurrencyProfile GetCurrencyData(string isoCode);

	/// <summary>
	/// Attempts to resolve regional defaults without throwing for an unavailable ISO code.
	/// Returns false and a null output when the code is blank or not in the offline catalog.
	/// </summary>
	public bool TryGetCurrencyData(string isoCode, out CurrencyProfile currencyProfile);
}
