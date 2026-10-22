using Business.Impl.Operations.Currency;
using Business.Models.Exceptions;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Contracts;
using Shared.Impl;

namespace UnitTests.Business.Operations.Currency;

/// <summary>
/// Verifies bundled currency lookup independently of persistence.
/// Checks unique permanent identities and common ISO currency availability.
/// Resolves the same profiles through list and individual lookups.
/// Covers trimmed and case-insensitive currency codes.
/// Rejects blank and unsupported codes with useful messages.
/// Checks that the nonthrowing lookup clears its output on failure.
/// Uses the deployed JSON asset and validates custom catalog content.
/// The operation does not depend on database configuration.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyOperationTests
{
	[Test]
	public void Catalog_ReturnsOneProfilePerCodeAndConsistentLookups()
	{
		using ServiceProvider provider = CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		CurrencyOperation operation = new(scope.ServiceProvider.GetRequiredService<ISharedContext>());
		List<CurrencyProfile> profiles = operation.GetListOfAvailableCurrencyData();
		Assert.That(profiles, Is.Not.Empty);
		Assert.That(profiles.Select(profile => profile.Code), Is.Unique);
		Assert.That(profiles.Select(profile => profile.Id), Is.Unique);
		Assert.That(profiles.All(profile => profile.Id != Guid.Empty), Is.True);
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
	[TestCase("Â¤Â¤")]
	[TestCase("XXX")]
	public void Lookup_RejectsInvalidCodeWithMessageAndTryReturnsFalse(string? code)
	{
		using ServiceProvider provider = CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		CurrencyOperation operation = new(scope.ServiceProvider.GetRequiredService<ISharedContext>());
		InvalidCurrencyCodeException? exception = Assert.Throws<InvalidCurrencyCodeException>(() => operation.GetCurrencyData(code!));
		Assert.That(exception!.Message, Does.Contain("currency"));
		Assert.That(exception.Message, Is.Not.EqualTo(code));
		Assert.That(operation.TryGetCurrencyData(code!, out CurrencyProfile found), Is.False);
		Assert.That(found, Is.Null);
	}

	[Test]
	public void Catalog_UsesFileIdentitiesAndCachesWithoutExposingMutableList()
	{
		const string json = """
			[{"id":"82997328-25ab-44db-b17e-1a136e6b30c3","code":"ZZZ","symbol":"Z","englishName":"Custom currency"}]
			""";
		IFileService files = Substitute.For<IFileService>();
		files.ReadAllText(Arg.Any<string>()).Returns(json);
		using ServiceProvider provider = CreateProvider(files);
		using IServiceScope scope = provider.CreateScope();
		CurrencyOperation operation = new(scope.ServiceProvider.GetRequiredService<ISharedContext>());
		List<CurrencyProfile> profiles = operation.GetListOfAvailableCurrencyData();
		profiles.Clear();
		Assert.That(operation.GetCurrencyData(" zZz ").Id, Is.EqualTo(Guid.Parse("82997328-25ab-44db-b17e-1a136e6b30c3")));
		Assert.That(operation.GetListOfAvailableCurrencyData(), Has.Count.EqualTo(1));
		Assert.That(operation.TryGetCurrencyData("USD", out _), Is.False);
		files.Received(1).ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "currencies.json"));
	}

	[TestCase("null")]
	[TestCase("[]")]
	[TestCase("[null]")]
	[TestCase("invalid json")]
	[TestCase("[{\"code\":\"EUR\",\"symbol\":\"E\",\"englishName\":\"Euro\"}]")]
	[TestCase("[{\"id\":\"00000000-0000-0000-0000-000000000000\",\"code\":\"EUR\",\"symbol\":\"E\",\"englishName\":\"Euro\"}]")]
	public void Catalog_RejectsInvalidProfiles(string json)
	{
		IFileService files = Substitute.For<IFileService>();
		files.ReadAllText(Arg.Any<string>()).Returns(json);
		using ServiceProvider provider = CreateProvider(files);
		using IServiceScope scope = provider.CreateScope();
		CurrencyOperation operation = new(scope.ServiceProvider.GetRequiredService<ISharedContext>());
		Assert.Throws<InvalidCurrencyException>(() => operation.GetListOfAvailableCurrencyData());
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Catalog_RejectsDuplicateIdentityOrCode(bool duplicateId)
	{
		Guid id = Guid.NewGuid();
		string json = System.Text.Json.JsonSerializer.Serialize(new[]
		{
			new CurrencyProfile { Id = id, Code = "EUR", Symbol = "E", EnglishName = "Euro" },
			new CurrencyProfile { Id = duplicateId ? id : Guid.NewGuid(), Code = duplicateId ? "USD" : "EUR", Symbol = "$", EnglishName = "Dollar" }
		});
		IFileService files = Substitute.For<IFileService>();
		files.ReadAllText(Arg.Any<string>()).Returns(json);
		using ServiceProvider provider = CreateProvider(files);
		using IServiceScope scope = provider.CreateScope();
		CurrencyOperation operation = new(scope.ServiceProvider.GetRequiredService<ISharedContext>());
		Assert.Throws<InvalidCurrencyException>(() => operation.GetListOfAvailableCurrencyData());
	}

	private static ServiceProvider CreateProvider(IFileService? files = null)
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		if (files is not null) { services.AddSingleton(files); }
		return services.BuildServiceProvider(true);
	}
}
