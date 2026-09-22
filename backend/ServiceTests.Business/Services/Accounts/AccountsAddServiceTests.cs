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
public sealed class AccountsAddServiceTests
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
	public async Task Add_ValidInput_PersistsFieldsAndReturnsGeneratedId()
	{
		Guid id = await _service.Add(_param);
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		Assert.That(id, Is.Not.EqualTo(Guid.Empty));
		_repository.Received(1).Add(Arg.Is<Account>(element =>
			element.Id == id && element.GroupId == _group.Id && ReferenceEquals(element.Group, _group) &&
			element.Name == _param.Name && element.Description == _param.Description &&
			element.IsFavorite && !element.IsDeleted && element.Order == 1 &&
			element.Original == now && element.Current == now));
		await _groupRepository.Received(1).GetWithContentsByIdAsync(_group.Id);
		await _unitOfWork.Received(1).SaveChangesAsync();
		_groupRepository.DidNotReceive().Update(Arg.Any<AccountGroup>());
	}

	[Test]
	public async Task Add_OptionalValues_PersistsNullAndFalse()
	{
		_param.Description = null;
		_param.IsFavorite = false;
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(element =>
			element.Description == null && !element.IsFavorite));
	}

	[Test]
	public async Task Add_ExistingElements_AppendsAfterMaximumIncludingDeleted()
	{
		Account first = new() { Id = Guid.NewGuid(), Name = "First", Order = 2 };
		Account deleted = new() { Id = Guid.NewGuid(), Name = "Deleted", Order = 9, IsDeleted = true };
		_group.Elements = [first, deleted];
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(element => element.Order == 10));
		Assert.That(first.Order, Is.EqualTo(2));
		Assert.That(deleted.Order, Is.EqualTo(9));
		_repository.DidNotReceive().Update(Arg.Any<Account>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_DuplicateElementName_RejectsIncludingDeleted(bool deleted)
	{
		_group.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = deleted });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_GroupAndChildWithSameName_DoesNotConflict()
	{
		_group.Name = _param.Name;
		_group.Children.Add(new AccountGroup { Id = Guid.NewGuid(), Name = _param.Name, Order = 100 });
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(element => element.Name == _param.Name && element.Order == 1));
	}

	[Test]
	public async Task Add_DifferentCase_UsesExactNameComparison()
	{
		_group.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _param.Name.ToUpperInvariant() });
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(element => element.Name == _param.Name));
	}

	[Test]
	public async Task Add_ToRoot_AllowsSelfParentGroup()
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		_group.Children.Add(_group);
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(element => element.GroupId == _group.Id));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Add_InvalidName_RejectsBeforeLookup(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_NullInput_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(null!));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_EmptyGroupId_RejectsBeforeLookup()
	{
		_param.GroupId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_MissingGroup_RejectsWithoutSaving()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((AccountGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_DeletedGroup_RejectsWithoutSaving()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_OrderOverflow_RejectsWithoutSaving()
	{
		_group.Elements.Add(new Account { Id = Guid.NewGuid(), Name = "Last", Order = int.MaxValue });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_LookupFails_PropagatesWithoutWrites()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_RepositoryFails_DoesNotSave()
	{
		_repository.When(repository => repository.Add(Arg.Any<Account>()))
			.Do(_ => throw new InvalidOperationException("Add failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
		AssertNoSave();
	}

	[Test]
	public void Add_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
	}

	[Test]
	public void Add_EmptyCurrency_Rejects()
	{
		_param.CurrencyId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_MissingOrDeletedCurrency_Rejects(bool deleted)
	{
		if (deleted)
		{
			_currency.IsDeleted = true;
		}
		else
		{
			_currencyRepository.GetByIdAsync(_currency.Id, CancellationToken.None).Returns((Currency?)null);
		}
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_AllReferences_SetsIdentifiersAndNavigationProperties()
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
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(account =>
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
	public void Add_InvalidOptionalReference_Rejects(string kind, string state)
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
			Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		}
		else
		{
			Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.Add(_param));
		}
		AssertNoWrites();
	}

	[Test]
	public void Add_CurrencyReadFailure_PropagatesWithoutWrites()
	{
		_currencyRepository.GetByIdAsync(_currency.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_NoOptionalReferences_StoresNulls()
	{
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<Account>(account =>
			account.CategoryId == null && account.Category == null &&
			account.CorrespondentId == null && account.Correspondent == null &&
			account.ProjectId == null && account.Project == null));
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
