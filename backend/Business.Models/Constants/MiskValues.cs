namespace Business.Models.Constants;

/// <summary>
/// Defines exceptional identifiers shared by bookkeeping validation.
/// NoCurrencyCode identifies the ISO code representing no currency.
/// Local catalog loading and Master validation reject that code.
/// Both paths use the same value to preserve dataset compatibility.
/// These identifiers are fixed domain values, not user settings.
/// Numeric limits and tracking markers belong to AppValues.
/// Storage names belong to StorageValues.
/// This class performs no validation or entity mutation.
/// </summary>
public static class MiskValues
{
	public const string NoCurrencyCode = "XXX";
}
