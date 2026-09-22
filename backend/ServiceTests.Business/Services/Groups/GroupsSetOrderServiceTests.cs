using Business.Contracts.Params;
using Business.Contracts.Services;
using Business.Contracts.Services.Base;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
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

namespace ServiceTests.Business.Services.Groups;

[TestFixture(typeof(AccountGroup), typeof(Account), typeof(IAccountGroupService), typeof(IAccountGroupRepository), Category = "Local")]
[TestFixture(typeof(CategoryGroup), typeof(Category), typeof(ICategoryGroupService), typeof(ICategoryGroupRepository), Category = "Local")]
[TestFixture(typeof(CorrespondentGroup), typeof(Correspondent), typeof(ICorrespondentGroupService), typeof(ICorrespondentGroupRepository), Category = "Local")]
[TestFixture(typeof(ProjectGroup), typeof(Project), typeof(IProjectGroupService), typeof(IProjectGroupRepository), Category = "Local")]
[TestFixture(typeof(TemplateGroup), typeof(Template), typeof(ITemplateGroupService), typeof(ITemplateGroupRepository), Category = "Local")]
public sealed class GroupsSetOrderServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IGroupService<TGroup, TElement, GroupParam>
	where TRepository : class, IGroupRepository<TGroup, TElement>
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private TService _service = null!;
	private TRepository _repository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private TGroup _parent = null!;
	private TGroup _group = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<TRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		switch (_repository)
		{
			case IAccountGroupRepository repository: _unitOfWork.AccountGroupRepo.Returns(repository); break;
			case ICategoryGroupRepository repository: _unitOfWork.CategoryGroupRepo.Returns(repository); break;
			case ICorrespondentGroupRepository repository: _unitOfWork.CorrespondentGroupRepo.Returns(repository); break;
			case IProjectGroupRepository repository: _unitOfWork.ProjectGroupRepo.Returns(repository); break;
			case ITemplateGroupRepository repository: _unitOfWork.TemplateGroupRepo.Returns(repository); break;
		}

		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unitOfWork);
		_provider = services.BuildServiceProvider(validateScopes: true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<TService>();
		_parent = new TGroup { Id = Guid.NewGuid(), Name = "Parent" };
		_group = new TGroup
		{
			Id = Guid.NewGuid(),
			ParentId = _parent.Id,
			Parent = _parent,
			Name = "Old name",
			Description = "Old description",
			Order = 7,
			Original = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			Current = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)
		};
		_parent.Children =
		[
			new TGroup { Id = Guid.NewGuid(), ParentId = _parent.Id, Order = 1 },
			new TGroup { Id = _group.Id, ParentId = _parent.Id, Order = 2 },
			new TGroup { Id = Guid.NewGuid(), ParentId = _parent.Id, Order = 3 }
		];
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(1, 1)]
	[TestCase(1, 2)]
	[TestCase(1, 3)]
	[TestCase(2, 1)]
	[TestCase(2, 2)]
	[TestCase(2, 3)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(3, 3)]
	public async Task SetOrder_ValidPosition_MovesTargetAndPersistsOnlyChangedSiblings(int from, int to)
	{
		List<TGroup> siblings = [.. _parent.Children];
		TGroup target = siblings[from - 1];
		_repository.GetByIdAsync(target.Id, CancellationToken.None).Returns(target);
		List<Guid> expected = [.. siblings.Select(child => child.Id)];
		expected.RemoveAt(from - 1);
		expected.Insert(to - 1, target.Id);
		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(child => child.Id, child => child.Order);
		DateTime before = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		foreach (TGroup sibling in siblings)
		{
			sibling.Name = "Keep name";
			sibling.Description = "Keep description";
			sibling.IsFavorite = true;
			sibling.Original = before;
			sibling.Current = before;
		}

		await _service.SetOrder(target.Id, to);

		Assert.That(siblings.OrderBy(child => child.Order).Select(child => child.Id), Is.EqualTo(expected));
		Assert.That(siblings.OrderBy(child => child.Order).Select(child => child.Order), Is.EqualTo((int[])[1, 2, 3]));
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		foreach (TGroup sibling in siblings)
		{
			if (sibling.Order == originalOrders[sibling.Id])
			{
				_repository.DidNotReceive().Update(Arg.Is<TGroup>(value => value.Id == sibling.Id));
				Assert.That(sibling.Current, Is.EqualTo(before));
			}
			else
			{
				_repository.Received(1).Update(sibling);
				Assert.That(sibling.Current, Is.EqualTo(now));
			}
			Assert.Multiple(() =>
			{
				Assert.That(sibling.Original, Is.EqualTo(before));
				Assert.That(sibling.ParentId, Is.EqualTo(_parent.Id));
				Assert.That(sibling.Name, Is.EqualTo("Keep name"));
				Assert.That(sibling.Description, Is.EqualTo("Keep description"));
				Assert.That(sibling.IsFavorite, Is.True);
				Assert.That(sibling.IsDeleted, Is.False);
			});
		}
		await _unitOfWork.Received(from == to ? 0 : 1).SaveChangesAsync();
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
	}

	[Test]
	public async Task SetOrder_SeparateTargetInstance_UpdatesTheLoadedSibling()
	{
		await _service.SetOrder(_group.Id, 1);
		TGroup target = _parent.Children.Single(child => child.Id == _group.Id);
		Assert.That(target.Order, Is.EqualTo(1));
		_repository.Received(1).Update(target);
		Assert.That(_group.Order, Is.EqualTo(7));
	}

	[Test]
	public async Task SetOrder_GappedOrders_NormalizesAndUsesRequestedPosition()
	{
		List<TGroup> siblings = [.. _parent.Children];
		siblings[0].Order = 10;
		siblings[1].Order = 30;
		siblings[2].Order = 90;
		await _service.SetOrder(_group.Id, 3);
		Assert.That(siblings.Select(child => child.Order), Is.EqualTo((int[])[1, 3, 2]));
		foreach (TGroup sibling in siblings)
		{
			_repository.Received(1).Update(sibling);
		}
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task SetOrder_DuplicateAndExtremeOrders_ProducesContiguousPositions()
	{
		List<TGroup> siblings = [.. _parent.Children];
		siblings[0].Order = 0;
		siblings[1].Order = 0;
		siblings[2].Order = int.MaxValue;
		await _service.SetOrder(_group.Id, 2);
		Assert.That(siblings.Single(child => child.Id == _group.Id).Order, Is.EqualTo(2));
		Assert.That(siblings.Select(child => child.Order).Order(), Is.EqualTo((int[])[1, 2, 3]));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task SetOrder_DeletedSiblingAndSelfParentRoot_AreExcluded()
	{
		TGroup deleted = new() { Id = Guid.NewGuid(), Order = 2, IsDeleted = true };
		_parent.Children.Add(deleted);
		_parent.ParentId = _parent.Id;
		_parent.Children.Add(_parent);
		await _service.SetOrder(_group.Id, 3);
		Assert.That(_parent.Order, Is.Zero);
		Assert.That(deleted.Order, Is.EqualTo(2));
		Assert.That(deleted.Current, Is.EqualTo(default(DateTime)));
		_repository.DidNotReceive().Update(_parent);
		_repository.DidNotReceive().Update(deleted);
	}

	[Test]
	public async Task SetOrder_OnlyChildAtPositionOne_DoesNotSave()
	{
		TGroup target = _parent.Children.Single(child => child.Id == _group.Id);
		target.Order = 1;
		_parent.Children = [target];
		await _service.SetOrder(_group.Id, 1);
		AssertNoWrites();
	}

	[TestCase(0)]
	[TestCase(-1)]
	[TestCase(int.MinValue)]
	public void SetOrder_NonpositivePosition_RejectsBeforeLookup(int order)
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetOrder(_group.Id, order));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_EmptyId_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetOrder(Guid.Empty, 1));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(4)]
	[TestCase(int.MaxValue)]
	public void SetOrder_PositionExceedsActiveCount_RejectsWithoutChanges(int order)
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), IsDeleted = true });
		_parent.Children.Add(_parent);
		int[] before = [.. _parent.Children.Select(child => child.Order)];
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetOrder(_group.Id, order));
		Assert.That(_parent.Children.Select(child => child.Order), Is.EqualTo(before));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_MissingGroup_RejectsWithoutSaving()
	{
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_DeletedGroup_RejectsWithoutSaving()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_Root_RejectsWithoutSaving()
	{
		_group.ParentId = _group.Id;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_MissingParent_RejectsWithoutSaving()
	{
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_DeletedParent_RejectsWithoutSaving()
	{
		_parent.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SetOrder_TargetAbsentFromActiveSiblings_RejectsWithoutSaving(bool deleted)
	{
		TGroup target = _parent.Children.Single(child => child.Id == _group.Id);
		if (deleted)
		{
			target.IsDeleted = true;
		}
		else
		{
			_parent.Children.Remove(target);
		}
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_ParentLookupFails_PropagatesFailureWithoutWrites()
	{
		_repository.GetWithChildrenByIdAsync(_parent.Id).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_group.Id, 1));
		AssertNoWrites();
	}

	[Test]
	public void SetOrder_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetOrder(_group.Id, 1));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
