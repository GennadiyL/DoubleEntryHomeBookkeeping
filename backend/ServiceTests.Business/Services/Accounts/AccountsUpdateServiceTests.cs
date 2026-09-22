using Business.Contracts.Params;
using Business.Contracts.Services;
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

[TestFixture(Category = "Local")]
public sealed class AccountsUpdateServiceTests
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
	private Account _element = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IAccountRepository>();
		_groupRepository = Substitute.For<IAccountGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_currencyRepository = Substitute.For<ICurrencyRepository>();
		_unitOfWork.CurrencyRepo.Returns(_currencyRepository);
		_currency = new Currency { Id = Guid.NewGuid(), IsoCode = "USD", Symbol = "$", Name = "US Dollar" };
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
		_element = new Account
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			CurrencyId = _currency.Id, Currency = _currency,
			Name = "Existing", Description = "Original description", Order = 1,
			Original = new DateTime(2020, 1, 1), Current = new DateTime(2020, 1, 2)
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
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
	public async Task Update_ValidInput_PersistsEditableFieldsOnly()
	{
		await _service.Update(_element.Id, _param);
		Assert.Multiple(() =>
		{
			Assert.That(_element.Name, Is.EqualTo(_param.Name));
			Assert.That(_element.Description, Is.EqualTo(_param.Description));
			Assert.That(_element.IsFavorite, Is.True);
			Assert.That(_element.IsDeleted, Is.False);
			Assert.That(_element.Order, Is.EqualTo(1));
			Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
			Assert.That(_element.Group, Is.SameAs(_group));
			Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(_element.Current, Is.EqualTo(Now));
		});
		_repository.Received(1).Update(_element);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task Update_UnchangedNameAndOptionalValues_AllowsSelf()
	{
		_param.Name = _element.Name;
		_param.Description = null;
		_param.IsFavorite = false;
		await _service.Update(_element.Id, _param);
		Assert.That(_element.Description, Is.Null);
		Assert.That(_element.IsFavorite, Is.False);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_DuplicateName_RejectsIncludingDeleted(bool deleted)
	{
		_group.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = deleted });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_NameComparison_IsOrdinalAndIgnoresGroupNames()
	{
		_group.Name = _param.Name;
		_group.Children.Add(new AccountGroup { Id = Guid.NewGuid(), Name = _param.Name });
		_group.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _param.Name.ToUpperInvariant() });
		await _service.Update(_element.Id, _param);
		Assert.That(_element.Name, Is.EqualTo(_param.Name));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void Update_InvalidName_RejectsBeforeReading(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Update_NullParam_Rejects()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, null!));
		AssertNoWrites();
	}

	[Test]
	public void Update_EmptyGroupId_Rejects()
	{
		_param.GroupId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_ChangedGroup_RejectsWithoutMoving()
	{
		_param.GroupId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Update_MissingGroup_Rejects()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((AccountGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_DeletedGroup_Rejects()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_EmptyId_RejectsBeforeReading()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(Guid.Empty, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_MissingOrDeletedElement_Rejects(bool deleted)
	{
		if (deleted)
		{
			_element.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns((Account?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_ReadFailure_PropagatesWithoutWrites()
	{
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_GroupReadFailure_PropagatesWithoutWrites()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_UpdateFailure_DoesNotSave()
	{
		_element.Order = 5;
		_repository.When(repository => repository.Update(Arg.Any<Account>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoSave();
	}

	[Test]
	public void Update_SaveFailure_Propagates()
	{
		_element.Order = 5;
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
	}

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

	[Test]
	public void Update_EmptyCurrency_Rejects()
	{
		_param.CurrencyId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_MissingOrDeletedCurrency_Rejects(bool deleted)
	{
		if (deleted)
		{
			_currency.IsDeleted = true;
		}
		else
		{
			_currencyRepository.GetByIdAsync(_currency.Id, CancellationToken.None).Returns((Currency?)null);
		}
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_AllReferences_SetsIdentifiersAndNavigationProperties()
	{
		Category category = new() { Id = Guid.NewGuid(), Name = "Category" };
		Correspondent correspondent = new() { Id = Guid.NewGuid(), Name = "Correspondent" };
		Project project = new() { Id = Guid.NewGuid(), Name = "Project" };
		_param.CategoryId = category.Id;
		_param.CorrespondentId = correspondent.Id;
		_param.ProjectId = project.Id;
		_unitOfWork.CategoryRepo.GetByIdAsync(category.Id, CancellationToken.None).Returns(category);
		_unitOfWork.CorrespondentRepo.GetByIdAsync(correspondent.Id, CancellationToken.None).Returns(correspondent);
		_unitOfWork.ProjectRepo.GetByIdAsync(project.Id, CancellationToken.None).Returns(project);
		await _service.Update(_element.Id, _param);
		_repository.Received(1).Update(Arg.Is<Account>(account =>
			account.CurrencyId == _currency.Id && ReferenceEquals(account.Currency, _currency) &&
			account.CategoryId == category.Id && ReferenceEquals(account.Category, category) &&
			account.CorrespondentId == correspondent.Id && ReferenceEquals(account.Correspondent, correspondent) &&
			account.ProjectId == project.Id && ReferenceEquals(account.Project, project)));
	}

	[TestCase("Category", "empty")]
	[TestCase("Category", "missing")]
	[TestCase("Category", "deleted")]
	[TestCase("Correspondent", "empty")]
	[TestCase("Correspondent", "missing")]
	[TestCase("Correspondent", "deleted")]
	[TestCase("Project", "empty")]
	[TestCase("Project", "missing")]
	[TestCase("Project", "deleted")]
	public void Update_InvalidOptionalReference_Rejects(string kind, string state)
	{
		Guid id = state == "empty" ? Guid.Empty : Guid.NewGuid();
		switch (kind)
		{
			case "Category":
				_param.CategoryId = id;
				_unitOfWork.CategoryRepo.GetByIdAsync(id, CancellationToken.None)
					.Returns(state == "deleted" ? new Category { Id = id, IsDeleted = true } : null);
				break;
			case "Correspondent":
				_param.CorrespondentId = id;
				_unitOfWork.CorrespondentRepo.GetByIdAsync(id, CancellationToken.None)
					.Returns(state == "deleted" ? new Correspondent { Id = id, IsDeleted = true } : null);
				break;
			case "Project":
				_param.ProjectId = id;
				_unitOfWork.ProjectRepo.GetByIdAsync(id, CancellationToken.None)
					.Returns(state == "deleted" ? new Project { Id = id, IsDeleted = true } : null);
				break;
		}
		if (state == "empty")
		{
			Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		}
		else
		{
			Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.Update(_element.Id, _param));
		}
		AssertNoWrites();
	}

	[Test]
	public void Update_CurrencyReadFailure_PropagatesWithoutWrites()
	{
		_currencyRepository.GetByIdAsync(_currency.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_NullReferences_ClearsExistingClassifications()
	{
		_element.Category = new Category { Id = Guid.NewGuid() };
		_element.CategoryId = _element.Category.Id;
		_element.Correspondent = new Correspondent { Id = Guid.NewGuid() };
		_element.CorrespondentId = _element.Correspondent.Id;
		_element.Project = new Project { Id = Guid.NewGuid() };
		_element.ProjectId = _element.Project.Id;
		await _service.Update(_element.Id, _param);
		Assert.Multiple(() =>
		{
			Assert.That(_element.CategoryId, Is.Null);
			Assert.That(_element.Category, Is.Null);
			Assert.That(_element.CorrespondentId, Is.Null);
			Assert.That(_element.Correspondent, Is.Null);
			Assert.That(_element.ProjectId, Is.Null);
			Assert.That(_element.Project, Is.Null);
		});
	}

	[Test]
	public async Task Update_UnusedAccount_CanChangeCurrencyDespiteTemplateUse()
	{
		Currency currency = NewCurrency();
		_unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(_element.Id).Returns(false);
		_unitOfWork.TemplateEntryRepo.GetByAccountIdAsync(_element.Id).Returns(new List<TemplateEntry>
		{
			new() { Id = Guid.NewGuid(), Template = new Template(), Account = _element, AccountId = _element.Id, Amount = 10 }
		});
		await _service.Update(_element.Id, _param);
		Assert.That(_element.CurrencyId, Is.EqualTo(currency.Id));
		Assert.That(_element.Currency, Is.SameAs(currency));
		await _unitOfWork.TransactionEntryRepo.Received(1).HasByAccountIdAsync(_element.Id);
		Assert.That(_unitOfWork.TemplateEntryRepo.ReceivedCalls(), Is.Empty);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void Update_UsedAccount_CannotChangeCurrencyOrOtherFields()
	{
		NewCurrency();
		_unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(_element.Id).Returns(true);
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.CurrencyId, Is.EqualTo(_currency.Id));
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		Assert.That(_element.Current, Is.EqualTo(new DateTime(2020, 1, 2)));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_UsedAccount_SameCurrencyAllowsOtherEdits()
	{
		_unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(_element.Id).Returns(true);
		await _service.Update(_element.Id, _param);
		Assert.That(_element.Name, Is.EqualTo(_param.Name));
		Assert.That(_element.CurrencyId, Is.EqualTo(_currency.Id));
		Assert.That(_unitOfWork.TransactionEntryRepo.ReceivedCalls(), Is.Empty);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void Update_UsageLookupFailure_DoesNotMutateOrSave()
	{
		NewCurrency();
		_unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.CurrencyId, Is.EqualTo(_currency.Id));
		AssertNoWrites();
	}

	private Currency NewCurrency()
	{
		Currency currency = new() { Id = Guid.NewGuid(), IsoCode = "EUR", Symbol = "€", Name = "Euro" };
		_param.CurrencyId = currency.Id;
		_currencyRepository.GetByIdAsync(currency.Id, CancellationToken.None).Returns(currency);
		return currency;
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<Account>());
		_repository.DidNotReceive().Update(Arg.Any<Account>());
		_groupRepository.DidNotReceive().Update(Arg.Any<AccountGroup>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
