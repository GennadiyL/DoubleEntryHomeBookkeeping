using Business.Contracts.Params;
using Business.Contracts.Services;
using Business.Contracts.Services.Base;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Contracts.Repositories.Base;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Elements;

[TestFixture(typeof(CategoryGroup), typeof(Category), typeof(ICategoryService), typeof(ICategoryRepository), typeof(ICategoryGroupRepository), Category = "Local")]
[TestFixture(typeof(CorrespondentGroup), typeof(Correspondent), typeof(ICorrespondentService), typeof(ICorrespondentRepository), typeof(ICorrespondentGroupRepository), Category = "Local")]
[TestFixture(typeof(ProjectGroup), typeof(Project), typeof(IProjectService), typeof(IProjectRepository), typeof(IProjectGroupRepository), Category = "Local")]
public sealed class ElementsCombineElementsServiceTests<TGroup, TElement, TService, TRepository, TGroupRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IElementService<TGroup, TElement, ElementParam>
	where TRepository : class, IElementRepository<TGroup, TElement>
	where TGroupRepository : class, IGroupRepository<TGroup, TElement>
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private TService _service = null!;
	private TRepository _repository = null!;
	private TGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private TGroup _group = null!;
	private IAccountRepository _accountRepository = null!;
	private List<Account> _accounts = null!;
	private TElement _destination = null!;
	private TElement _element = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<TRepository>();
		_groupRepository = Substitute.For<TGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_accountRepository = Substitute.For<IAccountRepository>();
		_unitOfWork.AccountRepo.Returns(_accountRepository);
		switch (_repository)
		{
			case ICategoryRepository repository:
				_unitOfWork.CategoryRepo.Returns(repository);
				_unitOfWork.CategoryGroupRepo.Returns((ICategoryGroupRepository)_groupRepository);
				break;
			case ICorrespondentRepository repository:
				_unitOfWork.CorrespondentRepo.Returns(repository);
				_unitOfWork.CorrespondentGroupRepo.Returns((ICorrespondentGroupRepository)_groupRepository);
				break;
			case IProjectRepository repository:
				_unitOfWork.ProjectRepo.Returns(repository);
				_unitOfWork.ProjectGroupRepo.Returns((IProjectGroupRepository)_groupRepository);
				break;
		}
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unitOfWork);
		_provider = services.BuildServiceProvider(validateScopes: true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<TService>();
		_group = new TGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_element = new TElement
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Existing", Description = "Original description", Order = 1,
			Original = new DateTime(2020, 1, 1), Current = new DateTime(2020, 1, 2)
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
		_destination = new TElement
		{
			Id = Guid.NewGuid(), Name = "Destination", GroupId = Guid.NewGuid(), Order = 4,
			Original = new DateTime(2020, 2, 1), Current = new DateTime(2020, 2, 2)
		};
		_repository.GetByIdAsync(_destination.Id, CancellationToken.None).Returns(_destination);
		_accounts = [];
		switch (_element)
		{
			case Category:
				_accountRepository.GetByCategoryIdAsync(_element.Id).Returns(_accounts);
				break;
			case Correspondent:
				_accountRepository.GetByCorrespondentIdAsync(_element.Id).Returns(_accounts);
				break;
			case Project:
				_accountRepository.GetByProjectIdAsync(_element.Id).Returns(_accounts);
				break;
		}
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task CombineElements_MatchingAccounts_ReplacesOnlyRelevantReferenceAndSoftDeletesSource()
	{
		Account active = AddAccount(false);
		Account deleted = AddAccount(true);
		await _service.CombineElements(_destination.Id, _element.Id);
		foreach (Account account in _accounts)
		{
			Assert.Multiple(() =>
			{
				Assert.That(account.CategoryId, Is.EqualTo(_element is Category ? _destination.Id : _element.Id));
				Assert.That(account.CorrespondentId, Is.EqualTo(_element is Correspondent ? _destination.Id : _element.Id));
				Assert.That(account.ProjectId, Is.EqualTo(_element is Project ? _destination.Id : _element.Id));
				Assert.That(account.Current, Is.EqualTo(Now));
				Assert.That(account.Original, Is.EqualTo(new DateTime(2020, 3, 1)));
				Assert.That(account.Name, Is.EqualTo("Account"));
				Assert.That(account.Description, Is.EqualTo("Description"));
				Assert.That(account.Order, Is.EqualTo(6));
				Assert.That(account.IsFavorite, Is.True);
				Assert.That(account.CurrencyId, Is.EqualTo(account.Currency.Id));
				Assert.That(account.GroupId, Is.EqualTo(account.Group.Id));
			});
			if (_destination is Category)
			{
				Assert.That(account.Category, Is.SameAs(_destination));
				Assert.That(account.Correspondent!.Id, Is.EqualTo(_element.Id));
				Assert.That(account.Project!.Id, Is.EqualTo(_element.Id));
			}
			else if (_destination is Correspondent)
			{
				Assert.That(account.Correspondent, Is.SameAs(_destination));
				Assert.That(account.Category!.Id, Is.EqualTo(_element.Id));
				Assert.That(account.Project!.Id, Is.EqualTo(_element.Id));
			}
			else
			{
				Assert.That(account.Project, Is.SameAs(_destination));
				Assert.That(account.Category!.Id, Is.EqualTo(_element.Id));
				Assert.That(account.Correspondent!.Id, Is.EqualTo(_element.Id));
			}
			_accountRepository.Received(1).Update(account);
		}
		Assert.That(active.IsDeleted, Is.False);
		Assert.That(deleted.IsDeleted, Is.True);
		Assert.That(_element.IsDeleted, Is.True);
		Assert.That(_element.Current, Is.EqualTo(Now));
		Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		Assert.That(_element.Order, Is.EqualTo(1));
		AssertDestinationUnchanged();
		_repository.Received(1).Update(_element);
		_repository.DidNotReceive().Update(_destination);
		await _unitOfWork.Received(1).SaveChangesAsync();
		Assert.That(_accountRepository.ReceivedCalls().Count(call => call.GetMethodInfo().Name.StartsWith("GetBy", StringComparison.Ordinal)), Is.EqualTo(1));
		Assert.That(_accountRepository.ReceivedCalls().Single(call => call.GetMethodInfo().Name.StartsWith("GetBy", StringComparison.Ordinal))
			.GetMethodInfo().Name, Is.EqualTo($"GetBy{typeof(TElement).Name}IdAsync"));
		Received.InOrder(() =>
		{
			_accountRepository.Update(active);
			_accountRepository.Update(deleted);
			_repository.Update(_element);
			_unitOfWork.SaveChangesAsync();
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CombineElements_NoReferences_DeletesSourceInSameOrDifferentGroup(bool sameGroup)
	{
		if (sameGroup)
		{
			_destination.GroupId = _element.GroupId;
		}
		_destination.Name = _element.Name;
		await _service.CombineElements(_destination.Id, _element.Id);
		Assert.That(_element.IsDeleted, Is.True);
		Assert.That(_destination.IsDeleted, Is.False);
		_accountRepository.DidNotReceive().Update(Arg.Any<Account>());
		_repository.Received(1).Update(_element);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task CombineElements_UnrelatedAccount_RemainsUntouched()
	{
		Account unrelated = AddAccount(false);
		_accounts.Clear();
		Guid? categoryId = unrelated.CategoryId;
		Guid? correspondentId = unrelated.CorrespondentId;
		Guid? projectId = unrelated.ProjectId;
		await _service.CombineElements(_destination.Id, _element.Id);
		Assert.That(unrelated.CategoryId, Is.EqualTo(categoryId));
		Assert.That(unrelated.CorrespondentId, Is.EqualTo(correspondentId));
		Assert.That(unrelated.ProjectId, Is.EqualTo(projectId));
		Assert.That(unrelated.Current, Is.EqualTo(new DateTime(2020, 3, 2)));
		_accountRepository.DidNotReceive().Update(Arg.Any<Account>());
	}

	[TestCase(true)]
	[TestCase(false)]
	public void CombineElements_EmptyIdentifier_RejectsBeforeReading(bool source)
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.CombineElements(
			source ? _destination.Id : Guid.Empty, source ? Guid.Empty : _element.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void CombineElements_SameIdentifier_RejectsBeforeReading()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.CombineElements(_element.Id, _element.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void CombineElements_MissingOrDeletedElement_Rejects(bool destination, bool deleted)
	{
		TElement target = destination ? _destination : _element;
		if (deleted)
		{
			target.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(target.Id, CancellationToken.None).Returns((TElement?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_accountRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CombineElements_ElementReadFailure_PropagatesWithoutWrites(bool destination)
	{
		_repository.GetByIdAsync(destination ? _destination.Id : _element.Id, CancellationToken.None)
			.ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_element.IsDeleted, Is.False);
		AssertNoWrites();
	}

	[Test]
	public void CombineElements_AccountReadFailure_PropagatesWithoutDeleting()
	{
		switch (_element)
		{
			case Category:
				_accountRepository.GetByCategoryIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
				break;
			case Correspondent:
				_accountRepository.GetByCorrespondentIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
				break;
			case Project:
				_accountRepository.GetByProjectIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
				break;
		}
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_element.IsDeleted, Is.False);
		AssertNoWrites();
	}

	[Test]
	public void CombineElements_AccountUpdateFailure_DoesNotDeleteOrSave()
	{
		AddAccount(false);
		_accountRepository.When(repository => repository.Update(Arg.Any<Account>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_element.IsDeleted, Is.False);
		_repository.DidNotReceive().Update(Arg.Any<TElement>());
		AssertNoSave();
	}

	[Test]
	public void CombineElements_SourceUpdateFailure_DoesNotSave()
	{
		AddAccount(false);
		_repository.When(repository => repository.Update(Arg.Any<TElement>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		AssertNoSave();
	}

	[Test]
	public void CombineElements_SaveFailure_Propagates()
	{
		AddAccount(false);
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
	}

	private Account AddAccount(bool deleted)
	{
		AccountGroup group = new() { Id = Guid.NewGuid() };
		Currency currency = new() { Id = Guid.NewGuid(), IsoCode = "USD", Symbol = "$", Name = "US Dollar" };
		Account account = new()
		{
			Id = Guid.NewGuid(), Name = "Account", Description = "Description", Order = 6,
			IsFavorite = true, IsDeleted = deleted, GroupId = group.Id, Group = group,
			CurrencyId = currency.Id, Currency = currency,
			CategoryId = _element.Id, Category = new Category { Id = _element.Id },
			CorrespondentId = _element.Id, Correspondent = new Correspondent { Id = _element.Id },
			ProjectId = _element.Id, Project = new Project { Id = _element.Id },
			Original = new DateTime(2020, 3, 1), Current = new DateTime(2020, 3, 2)
		};
		_accounts.Add(account);
		return account;
	}

	private void AssertDestinationUnchanged()
	{
		Assert.That(_destination.IsDeleted, Is.False);
		Assert.That(_destination.Name, Is.EqualTo("Destination"));
		Assert.That(_destination.Order, Is.EqualTo(4));
		Assert.That(_destination.Current, Is.EqualTo(new DateTime(2020, 2, 2)));
		Assert.That(_destination.Original, Is.EqualTo(new DateTime(2020, 2, 1)));
	}

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

	private void AssertNoWrites()
	{
		_accountRepository.DidNotReceive().Update(Arg.Any<Account>());
		_repository.DidNotReceive().Add(Arg.Any<TElement>());
		_repository.DidNotReceive().Update(Arg.Any<TElement>());
		_groupRepository.DidNotReceive().Update(Arg.Any<TGroup>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
