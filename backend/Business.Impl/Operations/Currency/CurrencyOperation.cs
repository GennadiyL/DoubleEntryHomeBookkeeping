using System.Text.Json;
using Business.Models.Exceptions;
using Shared.Contracts;

namespace Business.Impl.Operations.Currency;

/// <summary>
/// Resolves permanent currency identities from the bundled JSON catalog.
/// Loads Assets/currencies.json relative to the application output directory.
/// Uses shared file and JSON services without runtime regional enumeration.
/// Validates catalog identities and codes before exposing any profiles.
/// The first lookup loads the catalog once for this operation instance.
/// Returns independent lists containing immutable profile records.
/// Resolves trimmed codes without case sensitivity and reports unavailable codes.
/// Does not read or modify persisted currencies or exchange rates.
/// </summary>
internal sealed class CurrencyOperation : ICurrencyOperation
{
	private readonly ISharedContext _sharedContext;
	private readonly Lazy<List<CurrencyProfile>> _profiles;

	public CurrencyOperation(ISharedContext sharedContext)
	{
		_sharedContext = sharedContext;
		_profiles = new Lazy<List<CurrencyProfile>>(LoadProfiles);
	}

	public List<CurrencyProfile> GetListOfAvailableCurrencyData() => [.. _profiles.Value];

	public CurrencyProfile GetCurrencyData(string isoCode)
	{
		if (string.IsNullOrWhiteSpace(isoCode))
		{
			throw new InvalidCurrencyCodeException("A currency ISO code is required.");
		}
		if (!TryGetCurrencyData(isoCode, out CurrencyProfile profile))
		{
			throw new InvalidCurrencyCodeException($"The ISO currency code '{isoCode.Trim()}' is not available in the currency catalog.");
		}
		return profile;
	}

	public bool TryGetCurrencyData(string isoCode, out CurrencyProfile currencyProfile)
	{
		currencyProfile = null!;
		if (string.IsNullOrWhiteSpace(isoCode))
		{
			return false;
		}
		CurrencyProfile? profile = _profiles.Value
			.FirstOrDefault(item => string.Equals(item.Code, isoCode.Trim(), StringComparison.OrdinalIgnoreCase));
		if (profile is null)
		{
			return false;
		}
		currencyProfile = profile;
		return true;
	}

	private List<CurrencyProfile> LoadProfiles()
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Assets", "currencies.json");
		string json = _sharedContext.FileService.ReadAllText(path);
		List<CurrencyProfile>? profiles;
		try
		{
			profiles = _sharedContext.JsonService.Deserialize<List<CurrencyProfile>>(json);
		}
		catch (JsonException)
		{
			throw new InvalidCurrencyException("Assets/currencies.json must contain a valid currency profile array.");
		}
		if (profiles is null || profiles.Count == 0 || profiles.Any(profile => profile is null ||
			profile.Id == Guid.Empty || string.IsNullOrWhiteSpace(profile.Code) || profile.Code.Length != 3 ||
			profile.Code.Any(character => character is < 'A' or > 'Z') || profile.Code == "XXX" ||
			string.IsNullOrWhiteSpace(profile.Symbol) || string.IsNullOrWhiteSpace(profile.EnglishName)) ||
			profiles.Select(profile => profile.Id).Distinct().Count() != profiles.Count ||
			profiles.Select(profile => profile.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != profiles.Count)
		{
			throw new InvalidCurrencyException("Assets/currencies.json requires unique nonempty IDs, unique uppercase currency codes and display metadata.");
		}
		return profiles;
	}
}
