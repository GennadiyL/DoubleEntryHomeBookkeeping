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
public sealed class GroupsDeleteServiceTests<TGroup, TElement, TService, TRepository>
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
		_parent.Children.Add(_group);
		_repository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Delete_EmptyGroup_SoftDeletesAndPreservesOtherFields()
	{
		Guid id = _group.Id;
		DateTime original = _group.Original;
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		_group.IsFavorite = true;

		await _service.Delete(id);

		_repository.Received(1).Update(Arg.Is<TGroup>(group =>
			group.Id == id && group.IsDeleted && group.Current == now &&
			group.Original == original && group.ParentId == _parent.Id &&
			ReferenceEquals(group.Parent, _parent) && group.Order == 7 &&
			group.Name == "Old name" && group.Description == "Old description" && group.IsFavorite));
		await _repository.Received(1).GetWithContentsByIdAsync(id);
		await _unitOfWork.Received(1).SaveChangesAsync();
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
	}

	[Test]
	public void Delete_EmptyId_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Delete(Guid.Empty));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_MissingGroup_RejectsWithoutSaving()
	{
		_repository.GetWithContentsByIdAsync(_group.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Delete(_group.Id));
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_AlreadyDeleted_RejectsWithoutSaving()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Delete(_group.Id));
		AssertNoWrites(true);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_Root_RejectsEvenWhenEmpty(bool containsSelf)
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		if (containsSelf)
		{
			_group.Children.Add(_group);
		}
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Delete(_group.Id));
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_ActiveChildGroup_RejectsWithoutCascading()
	{
		TGroup child = new() { Id = Guid.NewGuid(), ParentId = _group.Id };
		_group.Children.Add(child);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Delete(_group.Id));
		Assert.That(child.IsDeleted, Is.False);
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_ActiveElement_RejectsWithoutCascading()
	{
		TElement element = new() { Id = Guid.NewGuid(), GroupId = _group.Id };
		_group.Elements.Add(element);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Delete(_group.Id));
		Assert.That(element.IsDeleted, Is.False);
		AssertNoWrites(false);
	}

	[Test]
	public async Task Delete_OnlyDeletedContents_AllowsDeletionWithoutChangingContents()
	{
		TGroup child = new() { Id = Guid.NewGuid(), IsDeleted = true };
		TElement element = new() { Id = Guid.NewGuid(), IsDeleted = true };
		_group.Children.Add(child);
		_group.Elements.Add(element);
		await _service.Delete(_group.Id);
		_repository.Received(1).Update(_group);
		Assert.That(child.Current, Is.EqualTo(default(DateTime)));
		Assert.That(element.Current, Is.EqualTo(default(DateTime)));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_MixedContents_RejectsWhenAnyContentIsActive(bool activeElement)
	{
		_group.Children.Add(new TGroup { Id = Guid.NewGuid(), IsDeleted = true });
		_group.Children.Add(new TGroup { Id = Guid.NewGuid(), IsDeleted = activeElement });
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), IsDeleted = true });
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), IsDeleted = !activeElement });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Delete(_group.Id));
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_LookupFails_PropagatesFailureWithoutWrites()
	{
		_repository.GetWithContentsByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_group.Id));
		AssertNoWrites(false);
	}

	[Test]
	public void Delete_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_group.Id));
	}

	private void AssertNoWrites(bool expectedDeleted)
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
		Assert.That(_group.IsDeleted, Is.EqualTo(expectedDeleted));
		Assert.That(_group.Current, Is.EqualTo(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
	}
}
