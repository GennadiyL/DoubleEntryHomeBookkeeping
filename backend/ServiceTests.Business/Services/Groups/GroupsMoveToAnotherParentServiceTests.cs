using Business.Models.Enums;
using Business.Models.Constants;
using Business.Contracts.Services;
using Business.Contracts.Utils.Merging;
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
using Shared.Impl;
using Tests.Common.DiConfigurations;
using Business.Contracts.Base.Services;

namespace ServiceTests.Business.Services.Groups;

[TestFixture(typeof(AccountGroup), typeof(Account), typeof(IAccountGroupService), typeof(IAccountGroupRepository), Category = "Local")]
[TestFixture(typeof(CategoryGroup), typeof(Category), typeof(ICategoryGroupService), typeof(ICategoryGroupRepository), Category = "Local")]
[TestFixture(typeof(CorrespondentGroup), typeof(Correspondent), typeof(ICorrespondentGroupService), typeof(ICorrespondentGroupRepository), Category = "Local")]
[TestFixture(typeof(ProjectGroup), typeof(Project), typeof(IProjectGroupService), typeof(IProjectGroupRepository), Category = "Local")]
[TestFixture(typeof(TemplateGroup), typeof(Template), typeof(ITemplateGroupService), typeof(ITemplateGroupRepository), Category = "Local")]
public sealed class GroupsMoveToAnotherParentServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IGroupService<TGroup, TElement>
	where TRepository : class, IGroupRepository<TGroup, TElement>
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private TService _service = null!;
	private TRepository _repository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private TGroup _parent = null!;
	private TGroup _group = null!;
	private TGroup _destination = null!;

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
			EditRevision = 1,
			ModificationType = ModificationType.None
		};
		_parent.Children.Add(_group);
		_parent.ParentId = _parent.Id;
		_parent.Parent = _parent;
		_destination = new TGroup { Id = Guid.NewGuid(), ParentId = _parent.Id, Name = "Destination" };
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
		_repository.GetByIdAsync(_parent.Id, CancellationToken.None).Returns(_parent);
		_repository.GetWithChildrenByIdAsync(_destination.Id).Returns(_destination);
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Move_ValidDestination_ChangesParentAndPreservesSubtreeAndOtherFields()
	{
		TGroup child = new() { Id = Guid.NewGuid(), ParentId = _group.Id };
		TElement element = new() { Id = Guid.NewGuid(), GroupId = _group.Id };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		_group.IsFavorite = true;
		Guid id = _group.Id;
		long? originalRevision = _group.EditRevision;

		await _service.MoveToAnotherParent(id, _destination.Id);

		_repository.Received(1).Update(Arg.Is<TGroup>(group =>
			group.Id == id && group.ParentId == _destination.Id &&
			ReferenceEquals(group.Parent, _destination) && group.Order == 0 &&
			group.ModificationType == (ModificationType.Content | ModificationType.Order) && group.EditRevision == originalRevision && group.IsFavorite &&
			group.Name == "Old name" && group.Description == "Old description" && !group.IsDeleted()));
		Assert.That(_group.Children.Single(), Is.SameAs(child));
		Assert.That(_group.Elements.Single(), Is.SameAs(element));
		Assert.That(child.ParentId, Is.EqualTo(id));
		Assert.That(element.GroupId, Is.EqualTo(id));
		_repository.DidNotReceive().Update(child);
		_repository.DidNotReceive().Update(_parent);
		_repository.DidNotReceive().Update(_destination);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task Move_SiblingsWithGaps_NormalizesAndExcludesDeleted()
	{
		TGroup sibling = new() { Id = Guid.NewGuid(), Name = "Sibling", Order = 3 };
		TGroup deleted = new() { Id = Guid.NewGuid(), Name = "Deleted", Order = 9, DeleteRevision = 0 };
		_destination.Children = [sibling, deleted];
		await _service.MoveToAnotherParent(_group.Id, _destination.Id);
		Assert.That(_group.Order, Is.EqualTo(1));
		Assert.That(sibling.Order, Is.Zero);
		Assert.That(deleted.Order, Is.EqualTo(9));
		_repository.Received(1).Update(sibling);
		_repository.DidNotReceive().Update(deleted);
	}

	[Test]
	public async Task Move_IntoRoot_IgnoresRootSelfReferenceForNameAndOrder()
	{
		_destination.ParentId = _destination.Id;
		_destination.Parent = _destination;
		_destination.Name = _group.Name;
		_destination.Order = int.MaxValue;
		_destination.Children.Add(_destination);
		await _service.MoveToAnotherParent(_group.Id, _destination.Id);
		Assert.That(_group.ParentId, Is.EqualTo(_destination.Id));
		Assert.That(_group.Order, Is.Zero);
	}

	[Test]
	public async Task Move_CurrentParent_DoesNotSave()
	{
		await _service.MoveToAnotherParent(_group.Id, _parent.Id);
		AssertUnchangedAndNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Move_EmptyIdentifier_RejectsBeforeLookup(bool emptyGroup)
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(
			emptyGroup ? Guid.Empty : _group.Id, emptyGroup ? _destination.Id : Guid.Empty));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_MissingOrDeletedGroup_Rejects(bool deleted)
	{
		if (deleted) { _group.DeleteRevision = 0; }
		else { _repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_Root_RejectsEveryDestination(bool sameDestination)
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(
			_group.Id, sameDestination ? _group.Id : _destination.Id));
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		AssertNoSave();
	}

	[Test]
	public void Move_IntoSelf_Rejects()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _group.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(1)]
	[TestCase(2)]
	[TestCase(8)]
	public void Move_IntoDescendant_RejectsCyclesAtAnyDepth(int depth)
	{
		TGroup ancestor = _destination;
		for (int i = 1; i < depth; i++)
		{
			TGroup next = new() { Id = Guid.NewGuid(), ParentId = _group.Id };
			ancestor.ParentId = next.Id;
			_repository.GetByIdAsync(next.Id, CancellationToken.None).Returns(next);
			ancestor = next;
		}
		ancestor.ParentId = _group.Id;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[Test]
	public void Move_DestinationAlreadyInCycle_RejectsWithoutLooping()
	{
		TGroup ancestor = new() { Id = Guid.NewGuid(), ParentId = _destination.Id };
		_destination.ParentId = ancestor.Id;
		_repository.GetByIdAsync(ancestor.Id, CancellationToken.None).Returns(ancestor);
		_repository.GetByIdAsync(_destination.Id, CancellationToken.None).Returns(_destination);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_MissingOrDeletedDestination_Rejects(bool deleted)
	{
		if (deleted) { _destination.DeleteRevision = 0; }
		else { _repository.GetWithChildrenByIdAsync(_destination.Id).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_MissingOrDeletedAncestor_Rejects(bool deleted)
	{
		if (deleted) { _parent.DeleteRevision = 0; }
		else { _repository.GetByIdAsync(_parent.Id, CancellationToken.None).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_DuplicateName_RejectsIncludingDeletedSibling(bool deleted)
	{
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _group.Name, DeleteRevision = deleted ? 0 : null });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase("OLD NAME")]
	[TestCase("  Old name  ")]
	public void Move_NormalizedDuplicateName_Rejects(string name)
	{
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = name });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[Test]
	public async Task Move_ExtremeOrder_NormalizesWithoutOverflow()
	{
		TGroup sibling = new() { Id = Guid.NewGuid(), Name = "Last", Order = int.MaxValue };
		_destination.Children.Add(sibling);
		await _service.MoveToAnotherParent(_group.Id, _destination.Id);
		Assert.That(sibling.Order, Is.Zero);
		Assert.That(_group.Order, Is.EqualTo(1));
	}

	[Test]
	public async Task Move_NormalizesBothCollectionsAndPreservesTracking(
		[Values(null, 0L, 7L)] long? revision,
		[Values(ModificationType.None, ModificationType.Content, ModificationType.Order,
			ModificationType.Content | ModificationType.Order)] ModificationType flags)
	{
		_group.EditRevision = revision;
		_group.ModificationType = flags;
		TGroup unchanged = new() { Id = Guid.NewGuid(), Name = "First", Order = 0 };
		TGroup shifted = new() { Id = Guid.NewGuid(), Name = "Next", Order = 8,
			EditRevision = 4, ModificationType = ModificationType.Content };
		TGroup deleted = new() { Id = Guid.NewGuid(), Order = 10, DeleteRevision = 0 };
		TGroup destinationChild = new() { Id = Guid.NewGuid(), Name = "Other", Order = 5,
			EditRevision = 3, ModificationType = ModificationType.Content };
		_parent.Children = [_parent, _group, unchanged, shifted, deleted];
		_destination.Children = [destinationChild];
		await _service.MoveToAnotherParent(_group.Id, _destination.Id);
		Assert.Multiple(() =>
		{
			Assert.That(_group.EditRevision, Is.EqualTo(revision));
			Assert.That(_group.DeleteRevision, Is.Null);
			Assert.That(_group.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
			Assert.That(_group.Order, Is.EqualTo(1));
			Assert.That(shifted.Order, Is.EqualTo(1));
			Assert.That(shifted.EditRevision, Is.EqualTo(4));
			Assert.That(shifted.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
			Assert.That(destinationChild.Order, Is.Zero);
			Assert.That(destinationChild.EditRevision, Is.EqualTo(3));
			Assert.That(destinationChild.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
			Assert.That(unchanged.ModificationType, Is.EqualTo(ModificationType.None));
			Assert.That(deleted.Order, Is.EqualTo(10));
		});
		_repository.Received(1).Update(shifted);
		_repository.Received(1).Update(destinationChild);
		_repository.DidNotReceive().Update(unchanged);
		_repository.DidNotReceive().Update(deleted);
		_repository.DidNotReceive().Update(_parent);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void Move_FixedRootId_RejectsEvenWithInvalidParent()
	{
		_group.Id = _group switch
		{
			AccountGroup => RootsIds.AccountGroupId,
			CategoryGroup => RootsIds.CategoryGroupId,
			CorrespondentGroup => RootsIds.CorrespondentGroupId,
			ProjectGroup => RootsIds.ProjectGroupId,
			TemplateGroup => RootsIds.TemplateGroupId,
			_ => throw new InvalidOperationException()
		};
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Move_InvalidSourceParent_RejectsBeforeChanges(bool deleted)
	{
		_destination.ParentId = _destination.Id;
		if (deleted) { _parent.DeleteRevision = 0; }
		else { _repository.GetWithChildrenByIdAsync(_parent.Id).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[Test]
	public void Move_AncestorLookupFails_PropagatesWithoutWrites()
	{
		_repository.GetByIdAsync(_parent.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
		AssertUnchangedAndNoWrites();
	}

	[Test]
	public void Move_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.MoveToAnotherParent(_group.Id, _destination.Id));
	}

	private void AssertUnchangedAndNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		AssertNoSave();
		Assert.That(_group.ParentId, Is.EqualTo(_parent.Id));
		Assert.That(_group.Parent, Is.SameAs(_parent));
		Assert.That(_group.Order, Is.EqualTo(7));
		Assert.That(_group.ModificationType, Is.EqualTo(ModificationType.None));
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
