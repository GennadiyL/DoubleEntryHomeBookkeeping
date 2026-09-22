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
public sealed class GroupsAddServiceTests<TGroup, TElement, TService, TRepository>
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
	public async Task Add_ValidInput_PersistsGroupAndReturnsItsId()
	{
		Guid id = await _service.Add(_param);
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

		_repository.Received(1).Add(Arg.Is<TGroup>(group =>
			group.Id == id && group.Id != Guid.Empty &&
			group.ParentId == _parent.Id && ReferenceEquals(group.Parent, _parent) &&
			group.Name == _param.Name && group.Description == _param.Description &&
			group.IsFavorite && !group.IsDeleted && group.Order == 1 &&
			group.Original == now && group.Current == now));
		await _unitOfWork.Received(1).SaveChangesAsync();
		await _repository.Received(1).GetWithChildrenByIdAsync(_parent.Id);
	}

	[Test]
	public async Task Add_SiblingsWithGaps_AppendsAfterMaximumOrder()
	{
		_parent.Children = [new TGroup { Id = Guid.NewGuid(), Name = "First", Order = 2 },
			new TGroup { Id = Guid.NewGuid(), Name = "Last", Order = 7 }];
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TGroup>(group => group.Order == 8));
		Assert.That(_parent.Children.Select(child => child.Order), Is.EqualTo((int[])[2, 7]));
	}

	[Test]
	public async Task Add_SelfParentRoot_DoesNotTreatRootAsItsOwnSibling()
	{
		_parent.Parent = _parent;
		_parent.ParentId = _parent.Id;
		_parent.Name = _param.Name;
		_parent.Order = 100;
		_parent.Children.Add(_parent);
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TGroup>(group => group.Order == 1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_DuplicateSibling_RejectsWithoutSaving(bool isDeleted)
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = isDeleted });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Add_InvalidName_RejectsBeforeRepositoryLookup(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Add(_param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_NullInput_RejectsBeforeRepositoryLookup()
	{
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Add(null!));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_EmptyParentId_RejectsBeforeRepositoryLookup()
	{
		_param.ParentId = Guid.Empty;
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Add(_param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_MissingParent_RejectsWithoutSaving()
	{
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_DeletedParent_RejectsWithoutSaving()
	{
		_parent.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_OrderOverflow_RejectsWithoutSaving()
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "Last", Order = int.MaxValue });
		Assert.ThrowsAsync<InvalidGroupException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_DifferentCase_PreservesExactNameComparison()
	{
		_parent.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = "SAVINGS" });
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TGroup>(group => group.Name == "Savings"));
	}

	[Test]
	public void Add_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
