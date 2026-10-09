using System.Globalization;
using Business.Models.Exceptions;

namespace Business.Impl.Operations.Currency;

/// <summary>
/// Resolves currency metadata from the runtime's regional culture catalog.
/// Enumerates local culture data without network or database access.
/// Skips neutral cultures and invalid region representations.
/// Retains the first name and symbol encountered for each ISO code.
/// Normalizes lookup codes by trimming and ignoring letter case.
/// Reports unsupported codes through a descriptive domain exception.
/// The nonthrowing lookup returns false for the same unsupported inputs.
/// Profiles contain display defaults rather than persisted currency state.
/// </summary>
internal sealed class CurrencyOperation : ICurrencyOperation
{
	public List<CurrencyProfile> GetListOfAvailableCurrencyData() =>
		[.. GetRegionInfos()
			.Where(region => region.ISOCurrencySymbol.Length == 3
				&& region.ISOCurrencySymbol.All(character => character is >= 'A' and <= 'Z')
				&& region.ISOCurrencySymbol != "XXX"
				&& !string.IsNullOrWhiteSpace(region.CurrencyEnglishName))
			.DistinctBy(region => region.ISOCurrencySymbol, StringComparer.OrdinalIgnoreCase)
			.Select(region => new CurrencyProfile
			{
				Code = region.ISOCurrencySymbol,
				Symbol = region.CurrencySymbol,
				EnglishName = region.CurrencyEnglishName
			})];

	public CurrencyProfile GetCurrencyData(string isoCode)
	{
		if (string.IsNullOrWhiteSpace(isoCode))
		{
			throw new InvalidCurrencyCodeException("A currency ISO code is required.");
		}
		if (!TryGetCurrencyData(isoCode, out CurrencyProfile profile))
		{
			throw new InvalidCurrencyCodeException($"The ISO currency code '{isoCode.Trim()}' is not available in the regional currency catalog.");
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
		CurrencyProfile? profile = GetListOfAvailableCurrencyData()
			.FirstOrDefault(item => string.Equals(item.Code, isoCode.Trim(), StringComparison.OrdinalIgnoreCase));
		if (profile is null)
		{
			return false;
		}
		currencyProfile = profile;
		return true;
	}

	private static IEnumerable<RegionInfo> GetRegionInfos()
	{
		foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
		{
			if (culture.IsNeutralCulture || string.IsNullOrEmpty(culture.Name))
			{
				continue;
			}
			RegionInfo region;
			try
			{
				region = new RegionInfo(culture.Name);
			}
			catch (ArgumentException)
			{
				continue;
			}
			yield return region;
		}
	}
}
