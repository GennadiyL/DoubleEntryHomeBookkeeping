namespace Business.Impl.Operations.Currency;

/// <summary>
/// Supplies offline currency profiles from the bundled JSON catalog.
/// Returns one permanent identity for each supported ISO currency code.
/// Catalog identities remain unchanged across installations and file updates.
/// Display metadata supplies defaults when creating a persistent currency.
/// Resolves trimmed ISO codes without case sensitivity.
/// Supports throwing and nonthrowing lookup variants.
/// Does not access persisted currencies or exchange rates.
/// The currency service uses these profiles for creation and selection.
/// </summary>
internal interface ICurrencyOperation
{
	/// <summary>
	/// Returns distinct currency profiles for the currency creation selector.
	/// Includes the permanent identity and display defaults for each currency.
	/// </summary>
	public List<CurrencyProfile> GetListOfAvailableCurrencyData();

	/// <summary>
	/// Resolves the catalog profile for a trimmed, case-insensitive ISO code.
	/// Throws a descriptive invalid-code exception when the code is blank or unavailable.
	/// </summary>
	public CurrencyProfile GetCurrencyData(string isoCode);

	/// <summary>
	/// Attempts to resolve catalog profile without throwing for an unavailable ISO code.
	/// Returns false and a null output when the code is blank or not in the offline catalog.
	/// </summary>
	public bool TryGetCurrencyData(string isoCode, out CurrencyProfile currencyProfile);
}
