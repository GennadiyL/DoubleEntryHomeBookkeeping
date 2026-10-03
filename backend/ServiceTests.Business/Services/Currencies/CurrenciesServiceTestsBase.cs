using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using DataAccess.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Currencies;

/// <summary>
/// Provides the currency service test environment through production DI.
/// Replaces repositories while retaining real configuration and currency operations.
/// Initializes a base currency and a live editable foreign currency.
/// Supplies valid System configuration for precision and deletion rules.
/// Each test receives isolated entities and substitute call history.
/// Catalog lists remain mutable to exercise order and deletion cases.
/// Rejected operations can be checked for any persistence calls.
/// Scope and provider lifetimes end after each test.
/// </summary>
public abstract class CurrenciesServiceTestsBase
{
	protected ICurrencyService Service { get; private set; } = null!;
	protected IAppUnitOfWork Unit { get; private set; } = null!;
	protected Currency BaseCurrency { get; private set; } = null!;
	protected Currency Currency { get; private set; } = null!;
	protected List<Currency> Currencies { get; private set; } = null!;
	protected SystemConfig Config { get; private set; } = null!;
	protected CurrencyParam Param { get; private set; } = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public void SetUp()
	{
		Unit = Substitute.For<IAppUnitOfWork>();
		Currencies = [];
		BaseCurrency = AddCurrency("USD", 0);
		Currency = AddCurrency("GBP", 1);
		Config = new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = BaseCurrency.Id, RatePrecision = 4 };
		Unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { Config });
		Unit.CurrencyRepo.GetAll(Arg.Any<CancellationToken>()).Returns(Currencies);
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => Unit);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		Service = _scope.ServiceProvider.GetRequiredService<ICurrencyService>();
		Param = new CurrencyParam { Code = " eur ", Name = "  Euro  ", Symbol = " EUR " };
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	protected Currency AddCurrency(string code, int order)
	{
		Currency currency = new() { Id = Guid.NewGuid(), Code = code, Name = code, Symbol = code, Order = order, EditRevision = 7 };
		Currencies.Add(currency);
		Unit.CurrencyRepo.GetById(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
		return currency;
	}

	protected void AssertNoWrites()
	{
		Unit.CurrencyRepo.DidNotReceiveWithAnyArgs().Add(default!);
		Unit.CurrencyRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Add(default!);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}
}
