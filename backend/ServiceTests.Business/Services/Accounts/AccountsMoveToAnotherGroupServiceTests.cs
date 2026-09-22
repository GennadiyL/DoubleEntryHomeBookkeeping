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
public sealed class AccountsMoveToAnotherGroupServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAccountService _service = null!;
	private IAccountRepository _repository = null!;
	private IAccountGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private AccountGroup _group = null!;
	private AccountGroup _destination = null!;
	private Account _element = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IAccountRepository>();
		_groupRepository = Substitute.For<IAccountGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
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
		_destination = new AccountGroup { Id = Guid.NewGuid(), Name = "Destination" };
		_groupRepository.GetWithContentsByIdAsync(_destination.Id).Returns(_destination);
		_group = new AccountGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_element = new Account
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Existing", Description = "Original description", Order = 1,
			Original = new DateTime(2020, 1, 1), Current = new DateTime(2020, 1, 2)
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task MoveToAnotherGroup_ValidMove_ChangesOnlyGroupOrderAndCurrent()
	{
		_destination.Elements.Add(new Account { Id = Guid.NewGuid(), Name = "Other", Order = 3 });
		_destination.Elements.Add(new Account { Id = Guid.NewGuid(), Name = "Deleted", Order = 7, IsDeleted = true });
		await _service.MoveToAnotherGroup(_element.Id, _destination.Id);
		Assert.Multiple(() =>
		{
			Assert.That(_element.GroupId, Is.EqualTo(_destination.Id));
			Assert.That(_element.Group, Is.SameAs(_destination));
			Assert.That(_element.Order, Is.EqualTo(8));
			Assert.That(_element.Current, Is.EqualTo(Now));
			Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(_element.Name, Is.EqualTo("Existing"));
			Assert.That(_element.Description, Is.EqualTo("Original description"));
			Assert.That(_element.IsFavorite, Is.False);
			Assert.That(_element.IsDeleted, Is.False);
			Assert.That(_destination.Elements.First().Order, Is.EqualTo(3));
			Assert.That(_destination.Elements.Last().Order, Is.EqualTo(7));
		});
		_repository.Received(1).Update(_element);
		await _unitOfWork.Received(1).SaveChangesAsync();
		_groupRepository.DidNotReceive().Update(Arg.Any<AccountGroup>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task MoveToAnotherGroup_EmptyDestination_IncludingRoot_StartsAtOne(bool root)
	{
		if (root)
		{
			_destination.ParentId = _destination.Id;
			_destination.Parent = _destination;
		}
		await _service.MoveToAnotherGroup(_element.Id, _destination.Id);
		Assert.That(_element.Order, Is.EqualTo(1));
		Assert.That(_element.GroupId, Is.EqualTo(_destination.Id));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task MoveToAnotherGroup_SameGroup_DoesNotWrite()
	{
		await _service.MoveToAnotherGroup(_element.Id, _group.Id);
		Assert.That(_element.Current, Is.EqualTo(new DateTime(2020, 1, 2)));
		Assert.That(_element.Group, Is.SameAs(_group));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MoveToAnotherGroup_DuplicateName_RejectsIncludingDeleted(bool deleted)
	{
		_destination.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _element.Name, IsDeleted = deleted });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertUnchanged();
	}

	[Test]
	public async Task MoveToAnotherGroup_DifferentCaseAndGroupNames_DoNotConflict()
	{
		_destination.Name = _element.Name;
		_destination.Children.Add(new AccountGroup { Id = Guid.NewGuid(), Name = _element.Name });
		_destination.Elements.Add(new Account { Id = Guid.NewGuid(), Name = _element.Name.ToUpperInvariant() });
		await _service.MoveToAnotherGroup(_element.Id, _destination.Id);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void MoveToAnotherGroup_EmptyIdentifier_RejectsBeforeReading(bool source)
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.MoveToAnotherGroup(
			source ? Guid.Empty : _element.Id, source ? _destination.Id : Guid.Empty));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertUnchanged();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MoveToAnotherGroup_MissingOrDeletedElement_Rejects(bool deleted)
	{
		if (deleted)
		{
			_element.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns((Account?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertUnchanged();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MoveToAnotherGroup_MissingOrDeletedDestination_Rejects(bool deleted)
	{
		if (deleted)
		{
			_destination.IsDeleted = true;
		}
		else
		{
			_groupRepository.GetWithContentsByIdAsync(_destination.Id).Returns((AccountGroup?)null);
		}
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertUnchanged();
	}

	[Test]
	public void MoveToAnotherGroup_OrderOverflow_Rejects()
	{
		_destination.Elements.Add(new Account { Id = Guid.NewGuid(), Name = "Last", Order = int.MaxValue });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertUnchanged();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void MoveToAnotherGroup_ReadFailure_PropagatesWithoutWrites(bool destination)
	{
		if (destination)
		{
			_groupRepository.GetWithContentsByIdAsync(_destination.Id).ThrowsAsync(new InvalidOperationException());
		}
		else
		{
			_repository.GetByIdAsync(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		}
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertUnchanged();
	}

	[Test]
	public void MoveToAnotherGroup_UpdateFailure_DoesNotSave()
	{
		_repository.When(repository => repository.Update(Arg.Any<Account>())).Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
		AssertNoSave();
	}

	[Test]
	public void MoveToAnotherGroup_SaveFailure_Propagates()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.MoveToAnotherGroup(_element.Id, _destination.Id));
	}

	private void AssertUnchanged()
	{
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		Assert.That(_element.Group, Is.SameAs(_group));
		Assert.That(_element.Order, Is.EqualTo(1));
		Assert.That(_element.Current, Is.EqualTo(new DateTime(2020, 1, 2)));
		AssertNoWrites();
	}

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

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
