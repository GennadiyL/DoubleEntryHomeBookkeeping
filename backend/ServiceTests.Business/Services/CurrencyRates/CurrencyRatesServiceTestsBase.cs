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

namespace ServiceTests.Business.Services.CurrencyRates;

/// <summary>
/// Supplies an isolated rate-service test environment.
/// Resolves the service and configuration operation through production DI.
/// Substitutes repositories and shared external dependencies.
/// Seeds a foreign currency, an account and valid System settings.
/// Rate records are supplied independently of navigation loading.
/// Each test starts with fresh data and repository call history.
/// Failed and read-only operations can be checked for any writes.
/// The service scope and provider are disposed after each case.
/// </summary>
public abstract class CurrencyRatesServiceTestsBase
{
	protected ICurrencyRateService Service { get; private set; } = null!;
	protected IAppUnitOfWork Unit { get; private set; } = null!;
	protected Currency Currency { get; private set; } = null!;
	protected Account Account { get; private set; } = null!;
	protected SystemConfig Config { get; private set; } = null!;
	protected CurrencyRateParam Param { get; private set; } = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public void SetUp()
	{
		Unit = Substitute.For<IAppUnitOfWork>();
		Currency = new Currency { Id = Guid.NewGuid(), Code = "EUR", Name = "Euro", Symbol = "EUR" };
		Account = new Account { Id = Guid.NewGuid(), Name = "Cash", CurrencyId = Currency.Id, Currency = Currency };
		Config = new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = Guid.NewGuid(), RatePrecision = 4 };
		Unit.CurrencyRepo.GetById(Currency.Id, Arg.Any<CancellationToken>()).Returns(Currency);
		Unit.AccountRepo.GetById(Account.Id, Arg.Any<CancellationToken>()).Returns(Account);
		Unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { Config });
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => Unit);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		Service = _scope.ServiceProvider.GetRequiredService<ICurrencyRateService>();
		Param = new CurrencyRateParam { CurrencyId = Currency.Id, Date = new DateOnly(2026, 10, 2), Rate = 1.23445m, Description = " Notes " };
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	protected CurrencyRate CreateRate(DateOnly date, decimal rate = 2m) => new()
	{
		Id = Guid.NewGuid(), Currency = Currency, CurrencyId = Currency.Id, Date = date, Rate = rate, EditRevision = 7
	};

	protected void AssertNoWrites()
	{
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Add(default!);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.CurrencyRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}
}
