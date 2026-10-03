using Business.Models.Enums;
using Business.Contracts.Services;
using Business.Contracts.Utils.Merging;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
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
public sealed class AccountsSetOrderServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAccountService _service = null!;
	private IAccountRepository _repository = null!;
	private IAccountGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private AccountGroup _group = null!;
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
		services.AddScoped<ICumulativeAmountCommand>(_ => Substitute.For<ICumulativeAmountCommand>());
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unitOfWork);
		_provider = services.BuildServiceProvider(validateScopes: true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<IAccountService>();
		_group = new AccountGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsById(_group.Id).Returns(_group);
		_element = new Account
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Existing", Description = "Original description", Order = 0,
			EditRevision = 1, ModificationType = ModificationType.None
		};
		_group.Elements.Add(_element);
		_repository.GetById(_element.Id, CancellationToken.None).Returns(_element);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(0, 2)]
	[TestCase(2, 0)]
	[TestCase(0, 1)]
	public async Task SetOrder_MovesElement_ShiftsOnlyAffectedActiveElements(int initial, int requested)
	{
		_element.Order = initial;
		Account second = AddSibling(initial == 0 ? 1 : 0);
		Account third = AddSibling(initial == 0 ? 2 : 1);
		Account deleted = AddSibling(20);
		deleted.DeleteRevision = 0;
		List<Account> expected = [.. _group.Elements.Where(item => !item.IsDeleted()).OrderBy(item => item.Order)];
		Dictionary<Guid, int> previous = expected.ToDictionary(item => item.Id, item => item.Order);
		expected.Remove(_element);
		expected.Insert(requested, _element);

		await _service.SetOrder(_element.Id, requested);

		for (int index = 0; index < expected.Count; index++)
		{
			Account item = expected[index];
			Assert.That(item.Order, Is.EqualTo(index));
			if (previous[item.Id] != item.Order)
			{
				Assert.That(item.ModificationType.HasFlag(ModificationType.Order), Is.True);
				_repository.Received(1).Update(item);
			}
			else
			{
				_repository.DidNotReceive().Update(item);
			}
		}
		Assert.That(deleted.Order, Is.EqualTo(20));
		Assert.That(deleted.ModificationType, Is.EqualTo(ModificationType.None));
		_repository.DidNotReceive().Update(deleted);
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		Assert.That(_element.EditRevision, Is.EqualTo(1));
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		await _unitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetOrder_NormalizesGapsAndTies_Deterministically()
	{
		_element.Order = 20;
		Account other = AddSibling(20);
		Account first = AddSibling(-1);
		await _service.SetOrder(_element.Id, 2);
		Assert.That(first.Order, Is.EqualTo(0));
		Assert.That(other.Order, Is.EqualTo(1));
		Assert.That(_element.Order, Is.EqualTo(2));
		await _unitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetOrder_DifferentLoadedInstance_UpdatesCollectionTarget()
	{
		Account loaded = new()
		{
			Id = _element.Id, GroupId = _group.Id, Name = _element.Name, Order = 0
		};
		_group.Elements = [loaded];
		AddSibling(1);
		await _service.SetOrder(_element.Id, 1);
		Assert.That(loaded.Order, Is.EqualTo(1));
		Assert.That(_element.Order, Is.EqualTo(0));
		_repository.Received(1).Update(Arg.Is<Account>(item => ReferenceEquals(item, loaded)));
	}

	[Test]
	public async Task SetOrder_Unchanged_DoesNotWrite()
	{
		AddSibling(1);
		await _service.SetOrder(_element.Id, 0);
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[TestCase(-1)]
	[TestCase(int.MinValue)]
	public void SetOrder_Negative_RejectsBeforeReading(int order)
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.SetOrder(_element.Id, order));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(1)]
	[TestCase(int.MaxValue)]
	public void SetOrder_BeyondActiveCount_Rejects(int order)
	{
		AddSibling(1).DeleteRevision = 0;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.SetOrder(_element.Id, order));
		Assert.That(_element.Order, Is.EqualTo(0));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SetOrder_MissingActiveMember_Rejects(bool deleted)
	{
		_group.Elements = deleted
			? [new Account { Id = _element.Id, Name = _element.Name, DeleteRevision = 0 }]
			: [];
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_MissingGroup_Rejects()
	{
		_groupRepository.GetWithContentsById(_group.Id).Returns((AccountGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_DeletedGroup_Rejects()
	{
		_group.DeleteRevision = 0;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_EmptyId_RejectsBeforeReading()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.SetOrder(Guid.Empty, 0));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SetOrder_MissingOrDeletedElement_Rejects(bool deleted)
	{
		if (deleted)
		{
			_element.DeleteRevision = 0;
		}
		else
		{
			_repository.GetById(_element.Id, CancellationToken.None).Returns((Account?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_ReadFailure_PropagatesWithoutWrites()
	{
		_repository.GetById(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_GroupReadFailure_PropagatesWithoutWrites()
	{
		_groupRepository.GetWithContentsById(_group.Id).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_UpdateFailure_DoesNotSave()
	{
		_element.Order = 5;
		_repository.When(repository => repository.Update(Arg.Any<Account>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_element.Id, 0));
		AssertNoSave();
	}

	[Test]
	public void SetOrder_SaveFailure_Propagates()
	{
		_element.Order = 5;
		_unitOfWork.SaveChanges().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_element.Id, 0));
	}


	private Account AddSibling(int order)
	{
		Account sibling = new()
		{
			Id = Guid.NewGuid(), Name = "Sibling", GroupId = _group.Id, Group = _group, Order = order,
			EditRevision = 1, ModificationType = ModificationType.None
		};
		_group.Elements.Add(sibling);
		return sibling;
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
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChanges)), Is.False);
}
