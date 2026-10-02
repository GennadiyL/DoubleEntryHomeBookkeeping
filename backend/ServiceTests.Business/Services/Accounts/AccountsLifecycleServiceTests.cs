using Business.Models.Enums;
using Business.Models.Entities.Config;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Utils.Merging;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Accounts;

/// <summary>
/// Verifies account lifecycle and editor reads through the service interface.
/// Uses production dependency injection and substituted repositories.
/// Covers creation tracking and permanently selected currencies.
/// Checks reference-protected deletion and balancing-account cleanup.
/// Verifies zero-based movement and aggregate merge synchronization flags.
/// Exercises every supported default-name component order and missing slots.
/// Checks detached read projections, failures and cancellation forwarding.
/// Persistence mechanics are verified separately against SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class AccountsLifecycleServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAccountService _service = null!;
	private IAccountRepository _repository = null!;
	private IAccountGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private AccountGroup _group = null!;
	private ICurrencyRepository _currencyRepository = null!;
	private Currency _currency = null!;
	private AccountParam _param = null!;
	private Account _account = null!;
	private SystemConfig _system = null!;
	private LocalConfig _local = null!;
	private Category _category = null!;
	private Correspondent _correspondent = null!;
	private Project _project = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IAccountRepository>();
		_groupRepository = Substitute.For<IAccountGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_currencyRepository = Substitute.For<ICurrencyRepository>();
		_unitOfWork.CurrencyRepo.Returns(_currencyRepository);
		_currency = new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", Name = "US Dollar" };
		_currencyRepository.GetByIdAsync(_currency.Id, CancellationToken.None).Returns(_currency);
		_unitOfWork.AccountRepo.Returns(_repository);
		_unitOfWork.AccountGroupRepo.Returns(_groupRepository);
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unitOfWork);
		_provider = services.BuildServiceProvider(validateScopes: true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<IAccountService>();
		_group = new AccountGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_groupRepository.GetByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(_group);
		_groupRepository.GetWithContentsByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(_group);
		_currencyRepository.GetByIdAsync(_currency.Id, Arg.Any<CancellationToken>()).Returns(_currency);
		_account = new Account { Id = Guid.NewGuid(), Name = "Existing", GroupId = _group.Id, Group = _group,
			CurrencyId = _currency.Id, Currency = _currency, Order = 0, EditRevision = 5 };
		_group.Elements.Add(_account);
		_repository.GetByIdAsync(_account.Id, Arg.Any<CancellationToken>()).Returns(_account);
		_system = new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = _currency.Id, EditRevision = 7 };
		_local = new LocalConfig { Id = Guid.NewGuid() };
		_unitOfWork.SystemConfigRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { _system });
		_unitOfWork.LocalConfigRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LocalConfig> { _local });
		_category = new Category { Id = Guid.NewGuid(), Name = "Cat" };
		_correspondent = new Correspondent { Id = Guid.NewGuid(), Name = "Corr" };
		_project = new Project { Id = Guid.NewGuid(), Name = "Proj" };
		_unitOfWork.CategoryRepo.GetByIdAsync(_category.Id, Arg.Any<CancellationToken>()).Returns(_category);
		_unitOfWork.CorrespondentRepo.GetByIdAsync(_correspondent.Id, Arg.Any<CancellationToken>()).Returns(_correspondent);
		_unitOfWork.ProjectRepo.GetByIdAsync(_project.Id, Arg.Any<CancellationToken>()).Returns(_project);
		_param = new AccountParam
		{
			GroupId = _group.Id,
			CurrencyId = _currency.Id,
			Name = "New element",
			Description = "Description",
			IsFavorite = true
		};
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Add_TrimsName_AllowsDuplicates_InitializesTracking()
	{
		_param.Name = "  Existing  ";
		_param.Description = "  Notes  ";

		await _service.Add(_param);

		_repository.Received(1).Add(Arg.Is<Account>(item => item.Name == "Existing" &&
			item.Description == "  Notes  " && item.Order == 1 &&
			item.EditRevision == null && item.DeleteRevision == null &&
			item.ModificationType == ModificationType.None));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(null)]
	[TestCase(0L)]
	[TestCase(8L)]
	public void Update_CurrencyIsImmutableRegardlessOfRevision(long? revision)
	{
		_account.EditRevision = revision;
		_param.CurrencyId = Guid.NewGuid();

		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_account.Id, _param));

		Assert.That(_account.CurrencyId, Is.EqualTo(_currency.Id));
		Assert.That(_account.Name, Is.EqualTo("Existing"));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_TrimsName_PreservesOrderAndRevision()
	{
		_param.Name = "  New name  ";
		_account.ModificationType = ModificationType.Order;

		await _service.Update(_account.Id, _param);

		Assert.That(_account.Name, Is.EqualTo("New name"));
		Assert.That(_account.EditRevision, Is.EqualTo(5));
		Assert.That(_account.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_AnyEntryReference_Rejects(bool transaction)
	{
		if (transaction) { _unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(_account.Id).Returns(true); }
		else
		{
			_unitOfWork.TemplateEntryRepo.GetByAccountIdAsync(_account.Id).Returns(new List<TemplateEntry>
			{
				new() { Account = _account, AccountId = _account.Id, Template = new Template { DeleteRevision = 0 } }
			});
		}

		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Delete(_account.Id));

		Assert.That(_account.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[Test]
	public async Task Delete_PreservesFlags_NormalizesSurvivorsAndClearsBalancingSelection(
		[Values(null, 0L, 7L)] long? revision,
		[Values(ModificationType.None, ModificationType.Content, ModificationType.Order,
			ModificationType.Content | ModificationType.Order)] ModificationType flags)
	{
		_account.EditRevision = revision;
		_account.ModificationType = flags;
		_system.BalancingAccountId = _account.Id;
		_system.ModificationType = ModificationType.Order;
		Account sibling = new() { Id = Guid.NewGuid(), Order = 8, EditRevision = 4, ModificationType = ModificationType.Content };
		Account deleted = new() { Id = Guid.NewGuid(), Order = 9, DeleteRevision = 0 };
		_group.Elements.Add(sibling);
		_group.Elements.Add(deleted);

		await _service.Delete(_account.Id);

		Assert.That(_account.DeleteRevision, Is.Zero);
		Assert.That(_account.EditRevision, Is.EqualTo(revision));
		Assert.That(_account.ModificationType, Is.EqualTo(flags));
		Assert.That(sibling.Order, Is.Zero);
		Assert.That(sibling.EditRevision, Is.EqualTo(4));
		Assert.That(sibling.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(deleted.Order, Is.EqualTo(9));
		Assert.That(_system.BalancingAccountId, Is.Null);
		Assert.That(_system.EditRevision, Is.EqualTo(7));
		Assert.That(_system.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		_unitOfWork.SystemConfigRepo.Received(1).Update(_system);
		_repository.DidNotReceive().Update(deleted);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Delete_OtherOrNoBalancingSelection_DoesNotUpdateConfiguration(bool selected)
	{
		Guid? id = selected ? Guid.NewGuid() : null;
		_system.BalancingAccountId = id;

		await _service.Delete(_account.Id);

		Assert.That(_system.BalancingAccountId, Is.EqualTo(id));
		_unitOfWork.SystemConfigRepo.DidNotReceive().Update(Arg.Any<SystemConfig>());
	}

	[Test]
	public void Delete_MissingConfiguration_FailsBeforeMutating()
	{
		_unitOfWork.SystemConfigRepo.GetAllAsync().Returns(new List<SystemConfig>());

		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_account.Id));

		Assert.That(_account.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[Test]
	public async Task Move_NormalizesBothGroups_PreservesCurrencyAndAddsBothFlags()
	{
		Account sibling = new() { Id = Guid.NewGuid(), Order = 9, ModificationType = ModificationType.Content };
		_group.Elements.Add(sibling);
		Account existing = new() { Id = Guid.NewGuid(), Name = _account.Name, Order = 8 };
		Account deleted = new() { Id = Guid.NewGuid(), Order = 4, DeleteRevision = 0 };
		AccountGroup destination = new() { Id = Guid.NewGuid(), Elements = [existing, deleted] };
		_groupRepository.GetWithContentsByIdAsync(destination.Id).Returns(destination);

		await _service.MoveToAnotherGroup(_account.Id, destination.Id);

		Assert.That(_account.CurrencyId, Is.EqualTo(_currency.Id));
		Assert.That(_account.GroupId, Is.EqualTo(destination.Id));
		Assert.That(_account.Order, Is.EqualTo(1));
		Assert.That(_account.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(_account.EditRevision, Is.EqualTo(5));
		Assert.That(sibling.Order, Is.Zero);
		Assert.That(sibling.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(existing.Order, Is.Zero);
		Assert.That(deleted.Order, Is.EqualTo(4));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Combine_EqualIds_ReturnsWithoutReads(bool empty)
	{
		Guid id = empty ? Guid.Empty : _account.Id;

		await _service.CombineElements(id, id);

		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public async Task Combine_ReplacesRepeatedEntries_TracksEachParentOnceAndClearsSelection()
	{
		Account destination = new() { Id = Guid.NewGuid(), CurrencyId = _currency.Id, GroupId = _group.Id,
			Order = 4, EditRevision = 6, ModificationType = ModificationType.Content };
		_group.Elements.Add(destination);
		_repository.GetByIdAsync(destination.Id).Returns(destination);
		_system.BalancingAccountId = _account.Id;
		_account.ModificationType = ModificationType.Order;
		Transaction parent = new() { Id = Guid.NewGuid(), EditRevision = 9, ModificationType = ModificationType.Order };
		Template template = new() { Id = Guid.NewGuid(), EditRevision = 8, DeleteRevision = 0, ModificationType = ModificationType.Order };
		TransactionEntry first = new() { Id = Guid.NewGuid(), TransactionId = parent.Id, Transaction = parent,
			AccountId = _account.Id, Account = _account, Amount = 10, Rate = 1.5m, Position = 0 };
		TransactionEntry second = new() { Id = Guid.NewGuid(), TransactionId = parent.Id, Transaction = parent,
			AccountId = _account.Id, Account = _account, Amount = -10, Rate = 1.5m, Position = 1 };
		TransactionEntry alreadyDestination = new() { Id = Guid.NewGuid(), TransactionId = parent.Id, Transaction = parent,
			AccountId = destination.Id, Account = destination, Amount = 4, Rate = 2, Position = 2 };
		TemplateEntry templateEntry = new() { Id = Guid.NewGuid(), TemplateId = template.Id, Template = template,
			AccountId = _account.Id, Account = _account, Amount = 3, Position = 4 };
		parent.Entries = [first, second, alreadyDestination];
		template.Entries = [templateEntry];
		_unitOfWork.TransactionEntryRepo.GetByAccountIdAsync(_account.Id).Returns(new List<TransactionEntry> { first, second });
		_unitOfWork.TemplateEntryRepo.GetByAccountIdAsync(_account.Id).Returns(new List<TemplateEntry> { templateEntry });
		_unitOfWork.TransactionRepo.GetByIdAsync(parent.Id).Returns(parent);
		_unitOfWork.TemplateRepo.GetByIdAsync(template.Id).Returns(template);

		await _service.CombineElements(destination.Id, _account.Id);

		Assert.That(parent.Entries, Has.Count.EqualTo(3));
		Assert.That(first.Account, Is.SameAs(destination));
		Assert.That(second.AccountId, Is.EqualTo(destination.Id));
		Assert.That(templateEntry.AccountId, Is.EqualTo(destination.Id));
		Assert.That((first.Amount, first.Rate, first.Position), Is.EqualTo((10m, 1.5m, 0)));
		Assert.That((second.Amount, second.Rate, second.Position), Is.EqualTo((-10m, 1.5m, 1)));
		Assert.That((templateEntry.Amount, templateEntry.Position), Is.EqualTo((3m, 4)));
		Assert.That(parent.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(template.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(parent.EditRevision, Is.EqualTo(9));
		Assert.That(template.EditRevision, Is.EqualTo(8));
		Assert.That(template.DeleteRevision, Is.Zero);
		Assert.That(_account.ModificationType, Is.EqualTo(ModificationType.Order));
		Assert.That(_account.DeleteRevision, Is.Zero);
		Assert.That(destination.Order, Is.Zero);
		Assert.That(destination.EditRevision, Is.EqualTo(6));
		Assert.That(_system.BalancingAccountId, Is.Null);
		_unitOfWork.TransactionRepo.Received(1).Update(parent);
		_unitOfWork.TemplateRepo.Received(1).Update(template);
		_unitOfWork.TransactionEntryRepo.DidNotReceive().Update(alreadyDestination);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void Combine_MissingParent_DoesNotRewriteEntries()
	{
		Account destination = new() { Id = Guid.NewGuid(), CurrencyId = _currency.Id };
		_repository.GetByIdAsync(destination.Id).Returns(destination);
		TransactionEntry entry = new() { TransactionId = Guid.NewGuid(), Transaction = null!, AccountId = _account.Id, Account = _account };
		_unitOfWork.TransactionEntryRepo.GetByAccountIdAsync(_account.Id).Returns(new List<TransactionEntry> { entry });

		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(destination.Id, _account.Id));

		Assert.That(entry.AccountId, Is.EqualTo(_account.Id));
		Assert.That(_account.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[TestCase(AccountNameOrder.CorrespondentCategoryProject, "Corr/Cat/Proj")]
	[TestCase(AccountNameOrder.CorrespondentProjectCategory, "Corr/Proj/Cat")]
	[TestCase(AccountNameOrder.CategoryCorrespondentProject, "Cat/Corr/Proj")]
	[TestCase(AccountNameOrder.CategoryProjectCorrespondent, "Cat/Proj/Corr")]
	[TestCase(AccountNameOrder.ProjectCorrespondentCategory, "Proj/Corr/Cat")]
	[TestCase(AccountNameOrder.ProjectCategoryCorrespondent, "Proj/Cat/Corr")]
	public async Task GetDefaultName_AllOrders_UsesCurrentClassificationNames(AccountNameOrder order, string expected)
	{
		_local.AccountNameOrder = order;

		string name = await _service.GetDefaultName(_correspondent.Id, _category.Id, _project.Id);

		Assert.That(name, Is.EqualTo(expected));
		Assert.That(_account.Name, Is.EqualTo("Existing"));
		AssertNoWrites();
	}

	[TestCase(false, false, false, "//")]
	[TestCase(true, false, false, "Corr//")]
	[TestCase(false, true, false, "/Cat/")]
	[TestCase(false, false, true, "//Proj")]
	[TestCase(true, true, false, "Corr/Cat/")]
	[TestCase(true, false, true, "Corr//Proj")]
	[TestCase(false, true, true, "/Cat/Proj")]
	public async Task GetDefaultName_AbsentSlots_RetainsSeparators(bool correspondent, bool category, bool project, string expected)
	{
		string name = await _service.GetDefaultName(correspondent ? _correspondent.Id : null,
			category ? _category.Id : null, project ? _project.Id : null);

		Assert.That(name, Is.EqualTo(expected));
		AssertNoWrites();
	}

	[Test]
	public async Task GetDefaultName_UsesCurrentSeparatorAndRenamedClassifications()
	{
		_local.DefaultAccountNameSeparator = "|";
		_category.Name = "Renamed";

		Assert.That(await _service.GetDefaultName(null, _category.Id, null), Is.EqualTo("|Renamed|"));

		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetDefaultName_MissingOrInvalidSettings_Fails(bool invalid)
	{
		if (invalid) { _local.AccountNameOrder = AccountNameOrder.Undefined; }
		else { _unitOfWork.LocalConfigRepo.GetAllAsync().Returns(new List<LocalConfig>()); }

		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetDefaultName(null, null, null));

		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetDefaultName_InvalidClassification_Rejects(bool deleted)
	{
		if (deleted) { _category.DeleteRevision = 0; }
		else { _unitOfWork.CategoryRepo.GetByIdAsync(_category.Id).Returns((Category?)null); }

		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.GetDefaultName(null, _category.Id, null));

		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task GetById_ReturnsDetachedEditorFields(bool classifications)
	{
		_account.CategoryId = classifications ? _category.Id : null;
		_account.CorrespondentId = classifications ? _correspondent.Id : null;
		_account.ProjectId = classifications ? _project.Id : null;
		_account.Description = "Notes";
		_account.IsFavorite = true;
		_account.Group = null!;
		_account.Currency = null!;

		AccountInfo info = await _service.GetById(_account.Id);

		Assert.Multiple(() =>
		{
			Assert.That(info.Id, Is.EqualTo(_account.Id));
			Assert.That(info.GroupId, Is.EqualTo(_group.Id));
			Assert.That(info.GroupName, Is.EqualTo(_group.Name));
			Assert.That(info.CurrencyId, Is.EqualTo(_currency.Id));
			Assert.That(info.CurrencyName, Is.EqualTo(_currency.Name));
			Assert.That(info.Name, Is.EqualTo("Existing"));
			Assert.That(info.Description, Is.EqualTo("Notes"));
			Assert.That(info.Order, Is.Zero);
			Assert.That(info.IsFavorite, Is.True);
			Assert.That(info.CategoryId, Is.EqualTo(classifications ? _category.Id : null));
			Assert.That(info.CategoryName, Is.EqualTo(classifications ? _category.Name : null));
			Assert.That(info.CorrespondentId, Is.EqualTo(classifications ? _correspondent.Id : null));
			Assert.That(info.CorrespondentName, Is.EqualTo(classifications ? _correspondent.Name : null));
			Assert.That(info.ProjectId, Is.EqualTo(classifications ? _project.Id : null));
			Assert.That(info.ProjectName, Is.EqualTo(classifications ? _project.Name : null));
		});
		info.Name = "DTO changed";
		Assert.That(_account.Name, Is.EqualTo("Existing"));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_UnknownOrEmpty_ThrowsNotFound(bool empty)
	{
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.GetById(empty ? Guid.Empty : Guid.NewGuid()));
		AssertNoWrites();
	}

	[TestCase(0L)]
	[TestCase(8L)]
	public void GetById_Deleted_ThrowsNotFound(long revision)
	{
		_account.DeleteRevision = revision;
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.GetById(_account.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_InvalidGroup_ThrowsNotFound(bool deleted)
	{
		if (deleted) { _group.DeleteRevision = 0; }
		else { _groupRepository.GetByIdAsync(_group.Id).Returns((AccountGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetById(_account.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_InvalidCurrency_ThrowsNotFound(bool deleted)
	{
		if (deleted) { _currency.DeleteRevision = 0; }
		else { _currencyRepository.GetByIdAsync(_currency.Id).Returns((Currency?)null); }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await _service.GetById(_account.Id));
		AssertNoWrites();
	}

	[Test]
	public async Task Reads_ForwardCancellationToRepositories()
	{
		using CancellationTokenSource cancellation = new();
		CancellationToken token = cancellation.Token;

		await _service.GetById(_account.Id, token);
		await _service.GetDefaultName(_correspondent.Id, _category.Id, _project.Id, token);

		await _repository.Received(1).GetByIdAsync(_account.Id, token);
		await _groupRepository.Received(1).GetByIdAsync(_group.Id, token);
		await _currencyRepository.Received(1).GetByIdAsync(_currency.Id, token);
		await _unitOfWork.LocalConfigRepo.Received(1).GetAllAsync(token);
		await _unitOfWork.CategoryRepo.Received(1).GetByIdAsync(_category.Id, token);
		await _unitOfWork.CorrespondentRepo.Received(1).GetByIdAsync(_correspondent.Id, token);
		await _unitOfWork.ProjectRepo.Received(1).GetByIdAsync(_project.Id, token);
		AssertNoWrites();
	}

	[Test]
	public void GetById_Cancellation_Propagates()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		_repository.GetByIdAsync(_account.Id, cancellation.Token).ThrowsAsync(new OperationCanceledException(cancellation.Token));

		Assert.ThrowsAsync<OperationCanceledException>(async () => await _service.GetById(_account.Id, cancellation.Token));

		AssertNoWrites();
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<Account>());
		_repository.DidNotReceive().Update(Arg.Any<Account>());
		_unitOfWork.SystemConfigRepo.DidNotReceive().Update(Arg.Any<SystemConfig>());
		_unitOfWork.TransactionEntryRepo.DidNotReceive().Update(Arg.Any<TransactionEntry>());
		_unitOfWork.TemplateEntryRepo.DidNotReceive().Update(Arg.Any<TemplateEntry>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
