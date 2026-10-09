using Business.Contracts.Services;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Accounts;

/// <summary>
/// Verifies currency-aware account-name generation through the service contract.
/// Uses production Business registration with substituted persistence.
/// Naming settings are read again for each preview operation.
/// Currency codes form a suffix without an intervening separator.
/// Missing classification slots retain their configured separators.
/// Both base and foreign currencies follow the same naming rules.
/// Invalid required currencies fail without writing any persistent data.
/// Disabled currency naming does not require or resolve a currency.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class AccountsGetDefaultNameServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAppUnitOfWork _unit = null!;
	private IAccountService _service = null!;
	private LocalConfig _config = null!;
	private Currency _currency = null!;

	[SetUp]
	public void SetUp()
	{
		_unit = Substitute.For<IAppUnitOfWork>();
		_config = new LocalConfig { AccountNameAddCurrency = true };
		_currency = new Currency { Id = Guid.NewGuid(), EnglishName = "Ukrainian Hryvnia", Code = "UAH", Name = "Hryvnia", Symbol = "₴" };
		_unit.LocalConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<LocalConfig> { _config });
		_unit.CurrencyRepo.GetById(_currency.Id, Arg.Any<CancellationToken>()).Returns(_currency);
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unit);
		services.AddScoped<ICumulativeAmountCommand>(_ => Substitute.For<ICumulativeAmountCommand>());
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<IAccountService>();
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	[TestCase("UAH", "/", "//(UAH)")]
	[TestCase("USD", "|", "||(USD)")]
	[TestCase("EUR", "/", "//(EUR)")]
	public async Task GetDefaultName_EmptySlots_UsesCodeSuffix(string code, string separator, string expected)
	{
		_currency.Code = code;
		_config.AccountNameSeparator = separator;
		using CancellationTokenSource source = new();

		Assert.That(await _service.GetDefaultName(null, null, null, _currency.Id, source.Token), Is.EqualTo(expected));
		await _unit.CurrencyRepo.Received(1).GetById(_currency.Id, source.Token);
		await _unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}

	[Test]
	public async Task GetDefaultName_ReadsCurrentOrderNamesAndFlag()
	{
		Correspondent correspondent = new() { Id = Guid.NewGuid(), Name = "Store" };
		Category category = new() { Id = Guid.NewGuid(), Name = "Food" };
		Project project = new() { Id = Guid.NewGuid(), Name = "Life" };
		_unit.CorrespondentRepo.GetById(correspondent.Id, Arg.Any<CancellationToken>()).Returns(correspondent);
		_unit.CategoryRepo.GetById(category.Id, Arg.Any<CancellationToken>()).Returns(category);
		_unit.ProjectRepo.GetById(project.Id, Arg.Any<CancellationToken>()).Returns(project);

		Assert.That(await _service.GetDefaultName(correspondent.Id, category.Id, project.Id, _currency.Id), Is.EqualTo("Store/Food/Life(UAH)"));
		_config.AccountNameOrder = AccountNameOrder.ProjectCategoryCorrespondent;
		Assert.That(await _service.GetDefaultName(correspondent.Id, category.Id, project.Id, _currency.Id), Is.EqualTo("Life/Food/Store(UAH)"));
		_config.AccountNameAddCurrency = false;
		Assert.That(await _service.GetDefaultName(correspondent.Id, category.Id, project.Id, null), Is.EqualTo("Life/Food/Store"));
		await _unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetDefaultName_MissingRequiredCurrency_Rejects(bool empty)
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await _service.GetDefaultName(null, null, null, empty ? Guid.Empty : null));
		_unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetDefaultName_UnknownOrDeletedCurrency_Rejects(bool deleted)
	{
		if (deleted) { _currency.DeleteRevision = 0; }
		else { _unit.CurrencyRepo.GetById(_currency.Id, Arg.Any<CancellationToken>()).Returns((Currency?)null); }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await _service.GetDefaultName(null, null, null, _currency.Id));
		_unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}

	[Test]
	public async Task GetDefaultName_FlagOff_DoesNotResolveCurrency()
	{
		_config.AccountNameAddCurrency = false;
		Assert.That(await _service.GetDefaultName(null, null, null, Guid.NewGuid()), Is.EqualTo("//"));
		Assert.That(await _service.GetDefaultName(null, null, null, null), Is.EqualTo("//"));
		await _unit.CurrencyRepo.DidNotReceiveWithAnyArgs().GetById(default, default);
	}
}
