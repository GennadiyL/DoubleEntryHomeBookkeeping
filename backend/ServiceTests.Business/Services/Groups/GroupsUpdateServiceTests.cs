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
public sealed class GroupsUpdateServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
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
	private GroupParam _param = null!;

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
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns(_parent);
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
		_param = new GroupParam
		{
			ParentId = _parent.Id,
			Name = "Savings",
			Description = "Household savings",
			IsFavorite = true
		};
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Update_ValidInput_ChangesEditableFieldsAndPreservesIdentityAndOrder()
	{
		Guid id = _group.Id;
		DateTime original = _group.Original;
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		await _service.Update(id, _param);

		_repository.Received(1).Update(Arg.Is<TGroup>(group =>
			group.Id == id && group.ParentId == _parent.Id &&
			ReferenceEquals(group.Parent, _parent) &&
			group.Name == _param.Name && group.Description == _param.Description &&
			group.IsFavorite && !group.IsDeleted && group.Order == 7 &&
			group.Original == original && group.Current == now));
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		await _unitOfWork.Received(1).SaveChangesAsync();
		await _repository.Received(1).GetByIdAsync(id, CancellationToken.None);
		await _repository.Received(1).GetWithChildrenByIdAsync(_parent.Id);
	}

	[Test]
	public async Task Update_UnchangedName_ExcludesCurrentGroupById()
	{
		_param.Name = _group.Name;
		_parent.Children = [new TGroup { Id = _group.Id, Name = _group.Name }];
		await _service.Update(_group.Id, _param);
		_repository.Received(1).Update(_group);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_DuplicateSibling_RejectsWithoutChangingGroup(bool isDeleted)
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = isDeleted });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(_group.Id, _param));
		AssertNoChangesOrWrites();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Update_InvalidName_RejectsBeforeLookup(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(_group.Id, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_NullInput_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(_group.Id, null!));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_EmptyId_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(Guid.Empty, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_EmptyParentId_RejectsBeforeLookup()
	{
		_param.ParentId = Guid.Empty;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(_group.Id, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_MissingGroup_RejectsWithoutSaving()
	{
		_repository.GetByIdAsync(_group.Id, CancellationToken.None).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_group.Id, _param));
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_DeletedGroup_RejectsWithoutSaving()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_group.Id, _param));
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_ChangedParent_RejectsWithoutSaving()
	{
		_param.ParentId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Update(_group.Id, _param));
		Assert.That(_group.ParentId, Is.EqualTo(_parent.Id));
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_MissingParent_RejectsWithoutSaving()
	{
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_group.Id, _param));
		AssertNoChangesOrWrites();
	}

	[Test]
	public void Update_DeletedParent_RejectsWithoutSaving()
	{
		_parent.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_group.Id, _param));
		AssertNoChangesOrWrites();
	}

	[Test]
	public async Task Update_DifferentCase_PreservesExactNameComparison()
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "SAVINGS" });
		await _service.Update(_group.Id, _param);
		_repository.Received(1).Update(Arg.Is<TGroup>(group => group.Name == "Savings"));
	}

	[Test]
	public async Task Update_ParentHasSameName_DoesNotTreatParentAsSibling()
	{
		_parent.Name = _param.Name;
		_parent.Children.Add(_parent);
		await _service.Update(_group.Id, _param);
		_repository.Received(1).Update(_group);
	}

	[Test]
	public async Task Update_RootWithSameNamedChild_PreservesSelfParent()
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		_group.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _param.Name });
		_param.ParentId = _group.Id;
		_repository.GetWithChildrenByIdAsync(_group.Id).Returns(_group);
		await _service.Update(_group.Id, _param);
		Assert.That(_group.ParentId, Is.EqualTo(_group.Id));
		Assert.That(_group.Parent, Is.SameAs(_group));
		_repository.Received(1).Update(_group);
	}

	[Test]
	public async Task Update_ClearsOptionalValues_PersistsNullAndFalse()
	{
		_group.IsFavorite = true;
		_param.IsFavorite = false;
		_param.Description = null;
		await _service.Update(_group.Id, _param);
		_repository.Received(1).Update(Arg.Is<TGroup>(group => !group.IsFavorite && group.Description == null));
	}

	[Test]
	public void Update_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_group.Id, _param));
	}

	private void AssertNoChangesOrWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
		Assert.Multiple(() =>
		{
			Assert.That(_group.Name, Is.EqualTo("Old name"));
			Assert.That(_group.Description, Is.EqualTo("Old description"));
			Assert.That(_group.IsFavorite, Is.False);
			Assert.That(_group.Order, Is.EqualTo(7));
			Assert.That(_group.Current, Is.EqualTo(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
		});
	}
}
