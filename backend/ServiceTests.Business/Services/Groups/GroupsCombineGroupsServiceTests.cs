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
using DataAccess.Core.Behaviors;
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
public sealed class GroupsCombineGroupsServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IGroupService<TGroup, TElement, GroupParam>
	where TRepository : class, IGroupRepository<TGroup, TElement>
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private TService _service = null!;
	private TRepository _repository = null!;
	private IRepository<TElement> _elementRepository = null!;
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
			case IAccountGroupRepository repository:
				_unitOfWork.AccountGroupRepo.Returns(repository);
				IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
				_unitOfWork.AccountRepo.Returns(accountRepository);
				_elementRepository = (IRepository<TElement>)accountRepository;
				break;
			case ICategoryGroupRepository repository:
				_unitOfWork.CategoryGroupRepo.Returns(repository);
				ICategoryRepository categoryRepository = Substitute.For<ICategoryRepository>();
				_unitOfWork.CategoryRepo.Returns(categoryRepository);
				_elementRepository = (IRepository<TElement>)categoryRepository;
				break;
			case ICorrespondentGroupRepository repository:
				_unitOfWork.CorrespondentGroupRepo.Returns(repository);
				ICorrespondentRepository correspondentRepository = Substitute.For<ICorrespondentRepository>();
				_unitOfWork.CorrespondentRepo.Returns(correspondentRepository);
				_elementRepository = (IRepository<TElement>)correspondentRepository;
				break;
			case IProjectGroupRepository repository:
				_unitOfWork.ProjectGroupRepo.Returns(repository);
				IProjectRepository projectRepository = Substitute.For<IProjectRepository>();
				_unitOfWork.ProjectRepo.Returns(projectRepository);
				_elementRepository = (IRepository<TElement>)projectRepository;
				break;
			case ITemplateGroupRepository repository:
				_unitOfWork.TemplateGroupRepo.Returns(repository);
				ITemplateRepository templateRepository = Substitute.For<ITemplateRepository>();
				_unitOfWork.TemplateRepo.Returns(templateRepository);
				_elementRepository = (IRepository<TElement>)templateRepository;
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
		_parent.Children.Add(_group);
		_parent.ParentId = _parent.Id;
		_parent.Parent = _parent;
		_destination = new TGroup { Id = Guid.NewGuid(), ParentId = _parent.Id, Name = "Destination" };
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
		_repository.GetByIdAsync(_parent.Id, CancellationToken.None).Returns(_parent);
		_repository.GetWithContentsByIdAsync(_destination.Id).Returns(_destination);
		_repository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_repository.GetWithContentsByIdAsync(_parent.Id).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Combine_MovesAllContentsAndSoftDeletesSourceInOneSave()
	{
		TGroup child = new() { Id = Guid.NewGuid(), ParentId = _group.Id, Parent = _group,
			Name = "Child", Description = "Keep", IsFavorite = true, Original = _group.Original };
		TGroup grandchild = new() { Id = Guid.NewGuid(), ParentId = child.Id };
		child.Children.Add(grandchild);
		TElement element = new() { Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Element", Description = "Keep element", IsFavorite = true, Original = _group.Original };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		DateTime original = _group.Original;
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

		await _service.CombineGroups(_destination.Id, _group.Id);

		Assert.Multiple(() =>
		{
			Assert.That(child.ParentId, Is.EqualTo(_destination.Id));
			Assert.That(child.Parent, Is.SameAs(_destination));
			Assert.That(element.GroupId, Is.EqualTo(_destination.Id));
			Assert.That(element.Group, Is.SameAs(_destination));
			Assert.That(child.Name, Is.EqualTo("Child"));
			Assert.That(element.Name, Is.EqualTo("Element"));
			Assert.That(child.Order, Is.EqualTo(1));
			Assert.That(element.Order, Is.EqualTo(1));
			Assert.That(child.Current, Is.EqualTo(now));
			Assert.That(element.Current, Is.EqualTo(now));
			Assert.That(child.Original, Is.EqualTo(original));
			Assert.That(element.Original, Is.EqualTo(original));
			Assert.That(child.Description, Is.EqualTo("Keep"));
			Assert.That(element.Description, Is.EqualTo("Keep element"));
			Assert.That(child.IsFavorite && element.IsFavorite, Is.True);
			Assert.That(child.Children.Single(), Is.SameAs(grandchild));
			Assert.That(grandchild.ParentId, Is.EqualTo(child.Id));
			Assert.That(_group.Children, Is.Empty);
			Assert.That(_group.Elements, Is.Empty);
			Assert.That(_group.IsDeleted, Is.True);
			Assert.That(_group.Current, Is.EqualTo(now));
			Assert.That(_group.Original, Is.EqualTo(original));
			Assert.That(_group.ParentId, Is.EqualTo(_parent.Id));
			Assert.That(_destination.Children, Does.Contain(child));
			Assert.That(_destination.Elements, Does.Contain(element));
		});
		_repository.Received(1).Update(child);
		_repository.Received(1).Update(_group);
		_repository.DidNotReceive().Update(grandchild);
		_elementRepository.Received(1).Update(element);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Combine_Conflicts_AppendSuffixUntilUniqueIncludingDeletedNames(bool deleted)
	{
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "Same", IsDeleted = deleted });
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "Same_1", IsDeleted = deleted });
		_destination.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Same", IsDeleted = deleted });
		_destination.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Same_1", IsDeleted = deleted });
		TGroup child = new() { Id = Guid.NewGuid(), Name = "Same" };
		TElement element = new() { Id = Guid.NewGuid(), Name = "Same" };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(child.Name, Is.EqualTo("Same_1_1"));
		Assert.That(element.Name, Is.EqualTo("Same_1_1"));
	}

	[Test]
	public async Task Combine_ConflictsBetweenIncomingItems_ProducesUniqueNames()
	{
		TGroup first = new() { Id = Guid.NewGuid(), Name = "Name", Order = 1 };
		TGroup second = new() { Id = Guid.NewGuid(), Name = "Name_1", Order = 2 };
		TElement firstElement = new() { Id = Guid.NewGuid(), Name = "Name", Order = 1 };
		TElement secondElement = new() { Id = Guid.NewGuid(), Name = "Name_1", Order = 2 };
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "Name" });
		_destination.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Name" });
		_group.Children = [first, second];
		_group.Elements = [firstElement, secondElement];
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(first.Name, Is.EqualTo("Name_1"));
		Assert.That(second.Name, Is.EqualTo("Name_1_1"));
		Assert.That(firstElement.Name, Is.EqualTo("Name_1"));
		Assert.That(secondElement.Name, Is.EqualTo("Name_1_1"));
	}

	[Test]
	public async Task Combine_NamesAreCaseSensitiveAndSeparateByType()
	{
		_destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "ITEM" });
		_destination.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Item" });
		TGroup child = new() { Id = Guid.NewGuid(), Name = "Item" };
		TElement element = new() { Id = Guid.NewGuid(), Name = "ITEM" };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(child.Name, Is.EqualTo("Item"));
		Assert.That(element.Name, Is.EqualTo("ITEM"));
	}

	[Test]
	public async Task Combine_DeletedContents_AreMovedWithoutRestoringThem()
	{
		TGroup child = new() { Id = Guid.NewGuid(), Name = "Deleted child", IsDeleted = true };
		TElement element = new() { Id = Guid.NewGuid(), Name = "Deleted element", IsDeleted = true };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(child.ParentId, Is.EqualTo(_destination.Id));
		Assert.That(element.GroupId, Is.EqualTo(_destination.Id));
		Assert.That(child.IsDeleted && element.IsDeleted, Is.True);
	}

	[Test]
	public async Task Combine_AppendsContentsInExistingOrderWithoutChangingDestinationItems()
	{
		TGroup existing = new() { Id = Guid.NewGuid(), Name = "Existing", Order = 8 };
		TElement existingElement = new() { Id = Guid.NewGuid(), Name = "Existing", Order = 12 };
		_destination.Children.Add(existing);
		_destination.Elements.Add(existingElement);
		TGroup first = new() { Id = Guid.NewGuid(), Name = "First", Order = 2 };
		TGroup last = new() { Id = Guid.NewGuid(), Name = "Last", Order = 9 };
		TElement firstElement = new() { Id = Guid.NewGuid(), Name = "First", Order = 2 };
		TElement lastElement = new() { Id = Guid.NewGuid(), Name = "Last", Order = 9 };
		_group.Children = [last, first];
		_group.Elements = [lastElement, firstElement];
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(first.Order, Is.EqualTo(9));
		Assert.That(last.Order, Is.EqualTo(10));
		Assert.That(firstElement.Order, Is.EqualTo(13));
		Assert.That(lastElement.Order, Is.EqualTo(14));
		Assert.That(existing.Order, Is.EqualTo(8));
		Assert.That(existingElement.Order, Is.EqualTo(12));
		_repository.DidNotReceive().Update(existing);
		_elementRepository.DidNotReceive().Update(existingElement);
	}

	[Test]
	public async Task Combine_EmptySource_StillDeletesSource()
	{
		await _service.CombineGroups(_destination.Id, _group.Id);
		Assert.That(_group.IsDeleted, Is.True);
		_repository.Received(1).Update(_group);
		_elementRepository.DidNotReceive().Update(Arg.Any<TElement>());
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task Combine_IntoParentRoot_AllowsAndExcludesRootSelfReference()
	{
		_parent.Children.Add(_parent);
		_parent.Order = int.MaxValue;
		TGroup child = new() { Id = Guid.NewGuid(), Name = _parent.Name };
		_group.Children.Add(child);
		await _service.CombineGroups(_parent.Id, _group.Id);
		Assert.That(child.ParentId, Is.EqualTo(_parent.Id));
		Assert.That(child.Name, Is.EqualTo(_parent.Name));
		Assert.That(_group.IsDeleted, Is.True);
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Combine_EmptyIdentifier_RejectsBeforeLookup(bool emptySource)
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(
			emptySource ? _destination.Id : Guid.Empty, emptySource ? Guid.Empty : _group.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Combine_SameGroup_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(_group.Id, _group.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(true, true)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(false, false)]
	public void Combine_MissingOrDeletedGroup_Rejects(bool source, bool deleted)
	{
		TGroup target = source ? _group : _destination;
		if (deleted) { target.IsDeleted = true; }
		else { _repository.GetWithContentsByIdAsync(target.Id).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		AssertNoWrites();
	}

	[Test]
	public void Combine_RootSource_Rejects()
	{
		_group.ParentId = _group.Id;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		AssertNoWrites();
	}

	[TestCase(1)]
	[TestCase(4)]
	public void Combine_IntoDescendant_RejectsCycle(int depth)
	{
		TGroup ancestor = _destination;
		for (int i = 1; i < depth; i++)
		{
			TGroup next = new() { Id = Guid.NewGuid() };
			ancestor.ParentId = next.Id;
			_repository.GetByIdAsync(next.Id, CancellationToken.None).Returns(next);
			ancestor = next;
		}
		ancestor.ParentId = _group.Id;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		AssertNoWrites();
	}

	[Test]
	public void Combine_ExistingDestinationCycle_RejectsWithoutLooping()
	{
		TGroup ancestor = new() { Id = Guid.NewGuid(), ParentId = _destination.Id };
		_destination.ParentId = ancestor.Id;
		_repository.GetByIdAsync(ancestor.Id, CancellationToken.None).Returns(ancestor);
		_repository.GetByIdAsync(_destination.Id, CancellationToken.None).Returns(_destination);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Combine_MissingOrDeletedAncestor_Rejects(bool deleted)
	{
		if (deleted) { _parent.IsDeleted = true; }
		else { _repository.GetByIdAsync(_parent.Id, CancellationToken.None).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Combine_OrderOverflow_RejectsBeforeChangingContents(bool elements)
	{
		TGroup child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = _group.Id };
		TElement element = new() { Id = Guid.NewGuid(), Name = "Element", GroupId = _group.Id };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		if (elements) { _destination.Elements.Add(new TElement { Id = Guid.NewGuid(), Order = int.MaxValue }); }
		else { _destination.Children.Add(new TGroup { Id = Guid.NewGuid(), Order = int.MaxValue }); }
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		Assert.That(child.ParentId, Is.EqualTo(_group.Id));
		Assert.That(element.GroupId, Is.EqualTo(_group.Id));
		Assert.That(_group.IsDeleted, Is.False);
		AssertNoWrites();
	}

	[Test]
	public void Combine_ElementUpdateFails_DoesNotSaveOrDeleteSource()
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Element" });
		_elementRepository.When(repository => repository.Update(Arg.Any<TElement>()))
			.Do(_ => throw new InvalidOperationException("Update failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
		Assert.That(_group.IsDeleted, Is.False);
		AssertNoSave();
	}

	[Test]
	public void Combine_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineGroups(_destination.Id, _group.Id));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		_elementRepository.DidNotReceive().Update(Arg.Any<TElement>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
