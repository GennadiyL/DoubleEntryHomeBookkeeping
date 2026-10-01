using Business.Models.Enums;
using Business.Contracts.Services.Trees;
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
public sealed class GroupsGetAllGroupsServiceTests<TGroup, TElement, TService, TRepository>
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
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
		_repository.GetByIdAsync(_parent.Id, CancellationToken.None).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task GetAllGroups_ReturnsFlatDetachedHierarchyWithRootOnce()
	{
		_parent.Id = _parent switch
		{
			AccountGroup => RootsIds.AccountGroupId,
			CategoryGroup => RootsIds.CategoryGroupId,
			CorrespondentGroup => RootsIds.CorrespondentGroupId,
			ProjectGroup => RootsIds.ProjectGroupId,
			TemplateGroup => RootsIds.TemplateGroupId,
			_ => throw new InvalidOperationException()
		};
		_parent.ParentId = _parent.Id;
		_parent.Children.Add(_parent);
		_parent.Order = 100;
		_group.ParentId = _parent.Id;
		_group.Parent = null!;
		_group.IsFavorite = true;
		_group.ModificationType = ModificationType.Content | ModificationType.Order;
		TGroup nested = new() { Id = Guid.NewGuid(), ParentId = _group.Id, Name = "Nested", Order = 0 };
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { nested, _group, _parent });
		List<GroupInfo> result = await _service.GetAllGroups();
		GroupInfo group = result.Single(item => item.Id == _group.Id);
		Assert.Multiple(() =>
		{
			Assert.That(result.Count, Is.EqualTo(3));
			Assert.That(result.Count(item => item.IsRoot), Is.EqualTo(1));
			Assert.That(result[0].Id, Is.EqualTo(_parent.Id));
			Assert.That(result[0].ParentId, Is.EqualTo(_parent.Id));
			Assert.That(result[0].ParentName, Is.EqualTo(_parent.Name));
			Assert.That(group.ParentId, Is.EqualTo(_parent.Id));
			Assert.That(group.ParentName, Is.EqualTo(_parent.Name));
			Assert.That(group.Name, Is.EqualTo(_group.Name));
			Assert.That(group.Description, Is.EqualTo(_group.Description));
			Assert.That(group.Order, Is.EqualTo(7));
			Assert.That(group.IsFavorite, Is.True);
			Assert.That(group.IsRoot, Is.False);
			Assert.That(result.Single(item => item.Id == nested.Id).ParentName, Is.EqualTo(_group.Name));
		});
		group.Name = "DTO edit";
		Assert.That(_group.Name, Is.EqualTo("Old name"));
		Assert.That(_group.EditRevision, Is.EqualTo(1));
		Assert.That(_group.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(_group.Order, Is.EqualTo(7));
		AssertReadOnly();
	}

	[Test]
	public async Task GetAllGroups_ExcludesPendingAndAcceptedDeletions()
	{
		_parent.ParentId = _parent.Id;
		TGroup pending = new() { Id = Guid.NewGuid(), DeleteRevision = 0 };
		TGroup deleted = new() { Id = Guid.NewGuid(), DeleteRevision = 9 };
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { pending, _group, deleted, _parent });
		List<GroupInfo> result = await _service.GetAllGroups();
		Assert.That(result.Select(group => group.Id), Is.EquivalentTo(new[] { _group.Id, _parent.Id }));
		AssertReadOnly();
	}

	[Test]
	public async Task GetAllGroups_OrdersSiblingsWithDeterministicTieBreakWithoutRenumbering()
	{
		_parent.ParentId = _parent.Id;
		TGroup first = new() { Id = Guid.Parse("00000001-0000-0000-0000-000000000000"), ParentId = _parent.Id, Order = 4 };
		TGroup second = new() { Id = Guid.Parse("80000000-0000-0000-0000-000000000000"), ParentId = _parent.Id, Order = 4 };
		_group.Order = 10;
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { _group, second, _parent, first });
		List<GroupInfo> result = await _service.GetAllGroups();
		Assert.That(result.Where(item => item.Id != _parent.Id).Select(item => item.Id), Is.EqualTo(new[] { first.Id, second.Id, _group.Id }));
		Assert.That(result.Where(item => item.Id != _parent.Id).Select(item => item.Order), Is.EqualTo((int[])[4, 4, 10]));
		AssertReadOnly();
	}

	[Test]
	public async Task GetAllGroups_EmptyRepository_ReturnsEmptyList()
	{
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup>());
		Assert.That(await _service.GetAllGroups(), Is.Empty);
		AssertReadOnly();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetAllGroups_MissingOrDeletedParent_RejectsIncompleteHierarchy(bool deleted)
	{
		_parent.DeleteRevision = 0;
		_repository.GetAllAsync(CancellationToken.None).Returns(deleted
			? new List<TGroup> { _group, _parent } : new List<TGroup> { _group });
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetAllGroups());
		AssertReadOnly();
	}

	[Test]
	public void GetAllGroups_ReadFailure_PropagatesWithoutSaving()
	{
		_repository.GetAllAsync(CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetAllGroups());
		AssertReadOnly();
	}

	private void AssertReadOnly()
	{
		Assert.That(_repository.ReceivedCalls().Count(), Is.EqualTo(1));
		Assert.That(_repository.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("GetAllAsync"));
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
