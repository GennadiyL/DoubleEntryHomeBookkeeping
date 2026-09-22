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
public sealed class GroupsSetFavoriteStatusServiceTests<TGroup, TElement, TService, TRepository>
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
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns(_group);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavoriteStatus_ChangedValue_PersistsFlagAndTimestampOnly(bool isFavorite)
	{
		_group.IsFavorite = !isFavorite;
		Guid id = _group.Id;
		DateTime original = _group.Original;
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		TGroup child = new() { Id = Guid.NewGuid(), IsFavorite = !isFavorite };
		TElement element = new() { Id = Guid.NewGuid(), IsFavorite = !isFavorite };
		_group.Children.Add(child);
		_group.Elements.Add(element);

		await _service.SetFavoriteStatus(id, isFavorite);

		_repository.Received(1).Update(Arg.Is<TGroup>(group =>
			group.Id == id && group.IsFavorite == isFavorite && group.Current == now &&
			group.Original == original && group.ParentId == _parent.Id &&
			ReferenceEquals(group.Parent, _parent) && group.Order == 7 &&
			group.Name == "Old name" && group.Description == "Old description" && !group.IsDeleted));
		await _repository.Received(1).GetByIdAsync(id, CancellationToken.None);
		await _unitOfWork.Received(1).SaveChangesAsync();
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(child);
		Assert.That(_group.Children.Single(), Is.SameAs(child));
		Assert.That(_group.Elements.Single(), Is.SameAs(element));
		Assert.That(child.IsFavorite, Is.EqualTo(!isFavorite));
		Assert.That(element.IsFavorite, Is.EqualTo(!isFavorite));
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavoriteStatus_UnchangedValue_DoesNotSaveOrChangeTimestamp(bool isFavorite)
	{
		_group.IsFavorite = isFavorite;
		DateTime current = _group.Current;
		await _service.SetFavoriteStatus(_group.Id, isFavorite);
		Assert.That(_group.Current, Is.EqualTo(current));
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
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetFavoriteStatus(_group.Id, isFavorite));
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void SetFavoriteStatus_DeletedGroup_RejectsEvenWhenValueMatches(bool isFavorite)
	{
		_group.IsDeleted = true;
		_group.IsFavorite = isFavorite;
		DateTime current = _group.Current;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.SetFavoriteStatus(_group.Id, isFavorite));
		Assert.That(_group.Current, Is.EqualTo(current));
		Assert.That(_group.IsFavorite, Is.EqualTo(isFavorite));
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavoriteStatus_Root_PreservesSelfParent(bool isFavorite)
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		_group.IsFavorite = !isFavorite;
		await _service.SetFavoriteStatus(_group.Id, isFavorite);
		Assert.That(_group.ParentId, Is.EqualTo(_group.Id));
		Assert.That(_group.Parent, Is.SameAs(_group));
		Assert.That(_group.IsFavorite, Is.EqualTo(isFavorite));
		_repository.Received(1).Update(_group);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void SetFavoriteStatus_LookupFails_PropagatesFailureWithoutWrites()
	{
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
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
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}

	[Test]
	public void SetFavoriteStatus_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SetFavoriteStatus(_group.Id, true));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
