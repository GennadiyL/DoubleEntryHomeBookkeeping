using Business.Impl.Operations.Currency;
using Business.Models.Exceptions;
using NUnit.Framework;

namespace UnitTests.Business.Operations.Currency;

/// <summary>
/// Verifies offline regional currency lookup independently of persistence.
/// Checks duplicate removal and common ISO currency availability.
/// Resolves the same profiles through list and individual lookups.
/// Covers trimmed and case-insensitive currency codes.
/// Rejects blank and unsupported codes with useful messages.
/// Checks that the nonthrowing lookup clears its output on failure.
/// Uses the installed runtime culture catalog without network access.
/// The operation does not depend on database configuration.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyOperationTests
{
	[Test]
	public void Catalog_ReturnsOneProfilePerCodeAndConsistentLookups()
	{
		CurrencyOperation operation = new();
		List<CurrencyProfile> profiles = operation.GetListOfAvailableCurrencyData();
		Assert.That(profiles, Is.Not.Empty);
		Assert.That(profiles.Select(profile => profile.Code), Is.Unique);
		Assert.That(profiles.All(profile => profile.Code.Length == 3
			&& profile.Code.All(character => character is >= 'A' and <= 'Z')
			&& profile.Code != "XXX" && !string.IsNullOrWhiteSpace(profile.EnglishName)), Is.True);
		CurrencyProfile usd = profiles.Single(profile => profile.Code == "USD");
		Assert.That(usd.EnglishName, Is.Not.Empty);
		Assert.That(usd.Symbol, Is.Not.Empty);
		Assert.That(operation.GetCurrencyData(" usd "), Is.EqualTo(usd));
		Assert.That(operation.TryGetCurrencyData(" UsD ", out CurrencyProfile found), Is.True);
		Assert.That(found, Is.EqualTo(usd));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	[TestCase("NOT-A-CURRENCY")]
	[TestCase("¤¤")]
	[TestCase("XXX")]
	public void Lookup_RejectsInvalidCodeWithMessageAndTryReturnsFalse(string? code)
	{
		CurrencyOperation operation = new();
		InvalidCurrencyCodeException? exception = Assert.Throws<InvalidCurrencyCodeException>(() => operation.GetCurrencyData(code!));
		Assert.That(exception!.Message, Does.Contain("currency"));
		Assert.That(exception.Message, Is.Not.EqualTo(code));
		Assert.That(operation.TryGetCurrencyData(code!, out CurrencyProfile found), Is.False);
		Assert.That(found, Is.Null);
	}
}
