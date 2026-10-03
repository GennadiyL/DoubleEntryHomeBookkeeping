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
public sealed class GroupsSetFavoriteStatusServiceTests<TGroup, TElement, TService, TRepository>
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
		_repository.GetById(_group.Id, CancellationToken.None).Returns(_group);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavoriteStatus_ChangedValue_PersistsFavoriteAndContentFlagOnly(bool isFavorite)
	{
		_group.IsFavorite = !isFavorite;
		Guid id = _group.Id;
		long? originalRevision = _group.EditRevision;
		TGroup child = new() { Id = Guid.NewGuid(), IsFavorite = !isFavorite };
		TElement element = new() { Id = Guid.NewGuid(), IsFavorite = !isFavorite };
		_group.Children.Add(child);
		_group.Elements.Add(element);

		await _service.SetFavoriteStatus(id, isFavorite);

		_repository.Received(1).Update(Arg.Is<TGroup>(group =>
			group.Id == id && group.IsFavorite == isFavorite && group.ModificationType == ModificationType.Content &&
			group.EditRevision == originalRevision && group.ParentId == _parent.Id &&
			ReferenceEquals(group.Parent, _parent) && group.Order == 7 &&
			group.Name == "Old name" && group.Description == "Old description" && !group.IsDeleted()));
		await _repository.Received(1).GetById(id, CancellationToken.None);
		await _unitOfWork.Received(1).SaveChanges();
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(child);
		Assert.That(_group.Children.Single(), Is.SameAs(child));
		Assert.That(_group.Elements.Single(), Is.SameAs(element));
		Assert.That(child.IsFavorite, Is.EqualTo(!isFavorite));
		Assert.That(element.IsFavorite, Is.EqualTo(!isFavorite));
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavoriteStatus_UnchangedValue_DoesNotSaveOrChangeTracking(bool isFavorite)
	{
		_group.IsFavorite = isFavorite;
		ModificationType previousModification = _group.ModificationType;
		await _service.SetFavoriteStatus(_group.Id, isFavorite);
		Assert.That(_group.ModificationType, Is.EqualTo(previousModification));
		Assert.That(_group.IsFavorite, Is.EqualTo(isFavorite));
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SetFavoriteStatus_EmptyId_RejectsBeforeLookup(bool isFavorite)
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetFavoriteStatus(Guid.Empty, isFavorite));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SetFavoriteStatus_MissingGroup_RejectsWithoutSaving(bool isFavorite)
	{
		_repository.GetById(_group.Id, CancellationToken.None).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetFavoriteStatus(_group.Id, isFavorite));
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_DeletedGroup_RejectsEvenWhenValueMatches([Values(false, true)] bool isFavorite, [Values(0L, 9L)] long revision)
	{
		_group.DeleteRevision = revision;
		_group.IsFavorite = isFavorite;
		ModificationType previousModification = _group.ModificationType;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetFavoriteStatus(_group.Id, isFavorite));
		Assert.That(_group.ModificationType, Is.EqualTo(previousModification));
		Assert.That(_group.IsFavorite, Is.EqualTo(isFavorite));
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_Root_RejectsWithoutChanges(
		[Values(false, true)] bool isFavorite, [Values(false, true)] bool selfParent)
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
		if (selfParent)
		{
			_group.ParentId = _group.Id;
			_group.Parent = _group;
		}
		_repository.GetById(_group.Id, CancellationToken.None).Returns(_group);
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.SetFavoriteStatus(_group.Id, isFavorite));
		Assert.That(_group.IsFavorite, Is.False);
		Assert.That(_group.EditRevision, Is.EqualTo(1));
		Assert.That(_group.DeleteRevision, Is.Null);
		Assert.That(_group.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[Test]
	public async Task SetFavoriteStatus_ChangedValue_PreservesRevisionAndExistingFlags(
		[Values(null, 0L, 7L)] long? revision,
		[Values(ModificationType.None, ModificationType.Content, ModificationType.Order,
			ModificationType.Content | ModificationType.Order)] ModificationType flags)
	{
		_group.EditRevision = revision;
		_group.ModificationType = flags;
		await _service.SetFavoriteStatus(_group.Id, true);
		Assert.That(_group.EditRevision, Is.EqualTo(revision));
		Assert.That(_group.DeleteRevision, Is.Null);
		Assert.That(_group.ModificationType, Is.EqualTo(flags | ModificationType.Content));
		_repository.Received(1).Update(_group);
		await _unitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetFavoriteStatus_UnchangedValue_PreservesExistingFlagsAndRevision(
		[Values(null, 0L, 7L)] long? revision,
		[Values(ModificationType.None, ModificationType.Content, ModificationType.Order,
			ModificationType.Content | ModificationType.Order)] ModificationType flags)
	{
		_group.EditRevision = revision;
		_group.ModificationType = flags;
		await _service.SetFavoriteStatus(_group.Id, false);
		Assert.That(_group.EditRevision, Is.EqualTo(revision));
		Assert.That(_group.DeleteRevision, Is.Null);
		Assert.That(_group.ModificationType, Is.EqualTo(flags));
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_LookupFails_PropagatesFailureWithoutWrites()
	{
		_repository.GetById(_group.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetFavoriteStatus(_group.Id, true));
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_UpdateFails_DoesNotSave()
	{
		_repository.When(repository => repository.Update(Arg.Any<TGroup>()))
			.Do(_ => throw new InvalidOperationException("Update failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetFavoriteStatus(_group.Id, true));
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChanges)), Is.False);
	}

	[Test]
	public void SetFavoriteStatus_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChanges().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetFavoriteStatus(_group.Id, true));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChanges)), Is.False);
	}
}
