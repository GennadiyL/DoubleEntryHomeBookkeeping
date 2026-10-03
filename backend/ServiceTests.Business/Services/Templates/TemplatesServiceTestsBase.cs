using Business.Contracts.Services;
using Business.Contracts.Services.Templates;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Supplies the common template service test environment.
/// Resolves the public service through production dependency injection.
/// Replaces repositories and the clock with deterministic substitutes.
/// Initializes a live group, template, account and currency.
/// Supplies configuration and persisted entries independently of navigation properties.
/// Each fixture receives fresh entities and substitutes for every test.
/// Checks that rejected operations and previews do not write or commit.
/// Disposes the service scope and provider after each case.
/// </summary>
public abstract class TemplatesServiceTestsBase
{
	protected ITemplateService Service { get; private set; } = null!;
	protected IAppUnitOfWork UnitOfWork { get; private set; } = null!;
	protected ITemplateRepository Repository { get; private set; } = null!;
	protected ITemplateGroupRepository GroupRepository { get; private set; } = null!;
	protected ITemplateEntryRepository EntryRepository { get; private set; } = null!;
	protected TemplateGroup Group { get; private set; } = null!;
	protected Template Template { get; private set; } = null!;
	protected Account Account { get; private set; } = null!;
	protected Currency Currency { get; private set; } = null!;
	protected SystemConfig Config { get; private set; } = null!;
	protected TemplateParam Param { get; private set; } = null!;
	protected List<TemplateEntry> Entries { get; private set; } = null!;
	protected DateTime Now { get; } = new(2026, 10, 2, 23, 30, 0, DateTimeKind.Utc);
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public void SetUp()
	{
		UnitOfWork = Substitute.For<IAppUnitOfWork>();
		Repository = Substitute.For<ITemplateRepository>();
		GroupRepository = Substitute.For<ITemplateGroupRepository>();
		EntryRepository = Substitute.For<ITemplateEntryRepository>();
		UnitOfWork.TemplateRepo.Returns(Repository);
		UnitOfWork.TemplateGroupRepo.Returns(GroupRepository);
		UnitOfWork.TemplateEntryRepo.Returns(EntryRepository);
		IDateTimeService clock = Substitute.For<IDateTimeService>();
		clock.UtcNow.Returns(Now);
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IDateTimeService>(_ => clock);
		services.AddScoped<IAppUnitOfWork>(_ => UnitOfWork);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		Service = _scope.ServiceProvider.GetRequiredService<ITemplateService>();
		Group = new TemplateGroup { Id = Guid.NewGuid(), Name = "Group" };
		GroupRepository.GetWithContentsById(Group.Id, Arg.Any<CancellationToken>()).Returns(Group);
		Template = AddSibling("Existing", 0);
		Template.EditRevision = 7;
		Template.Description = "Description";
		Currency = new Currency { Id = Guid.NewGuid(), Name = "Dollar", Code = "USD", Symbol = "$" };
		Account = new Account { Id = Guid.NewGuid(), Name = "Cash", CurrencyId = Currency.Id, Currency = Currency };
		UnitOfWork.AccountRepo.GetById(Account.Id, Arg.Any<CancellationToken>()).Returns(Account);
		UnitOfWork.CurrencyRepo.GetById(Currency.Id, Arg.Any<CancellationToken>()).Returns(Currency);
		Config = new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = Currency.Id };
		UnitOfWork.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { Config });
		Entries = [new TemplateEntry
		{
			Id = Guid.NewGuid(), TemplateId = Template.Id, Template = Template,
			AccountId = Account.Id, Account = Account, Amount = 10m, Position = 0
		}];
		EntryRepository.GetByTemplateId(Template.Id, Arg.Any<CancellationToken>()).Returns(Entries);
		Param = new TemplateParam { Name = "  New  ", GroupId = Group.Id, Description = "  Notes  ", IsFavorite = true };
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	protected Template AddSibling(string name, int order)
	{
		Template item = new() { Id = Guid.NewGuid(), Name = name, GroupId = Group.Id, Group = Group, Order = order };
		Group.Elements.Add(item);
		Repository.GetById(item.Id, Arg.Any<CancellationToken>()).Returns(item);
		return item;
	}

	protected void AddInput(decimal amount)
	{
		Param.Entries.Add(new TemplateEntryParam { AccountId = Account.Id, Amount = amount });
	}

	protected void AssertNoWrites()
	{
		Repository.DidNotReceiveWithAnyArgs().Add(default!);
		Repository.DidNotReceiveWithAnyArgs().Update(default!);
		EntryRepository.DidNotReceiveWithAnyArgs().Add(default!);
		EntryRepository.DidNotReceiveWithAnyArgs().Update(default!);
		EntryRepository.DidNotReceiveWithAnyArgs().RemoveRange(default!);
		UnitOfWork.TransactionRepo.DidNotReceiveWithAnyArgs().Add(default!);
		UnitOfWork.TransactionRepo.DidNotReceiveWithAnyArgs().Update(default!);
		UnitOfWork.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}
}
