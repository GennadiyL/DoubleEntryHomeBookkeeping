using Business.Contracts.Params;
using Business.Contracts.Services;
using Business.Contracts.Services.Base;
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
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Elements;

[TestFixture(typeof(CategoryGroup), typeof(Category), typeof(ICategoryService), typeof(ICategoryRepository), typeof(ICategoryGroupRepository), Category = "Local")]
[TestFixture(typeof(CorrespondentGroup), typeof(Correspondent), typeof(ICorrespondentService), typeof(ICorrespondentRepository), typeof(ICorrespondentGroupRepository), Category = "Local")]
[TestFixture(typeof(ProjectGroup), typeof(Project), typeof(IProjectService), typeof(IProjectRepository), typeof(IProjectGroupRepository), Category = "Local")]
public sealed class ElementsAddServiceTests<TGroup, TElement, TService, TRepository, TGroupRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IElementService<TGroup, TElement, ElementParam>
	where TRepository : class, IElementRepository<TGroup, TElement>
	where TGroupRepository : class, IGroupRepository<TGroup, TElement>
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private TService _service = null!;
	private TRepository _repository = null!;
	private TGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private TGroup _group = null!;
	private ElementParam _param = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<TRepository>();
		_groupRepository = Substitute.For<TGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		switch (_repository)
		{
			case ICategoryRepository repository:
				_unitOfWork.CategoryRepo.Returns(repository);
				_unitOfWork.CategoryGroupRepo.Returns((ICategoryGroupRepository)_groupRepository);
				break;
			case ICorrespondentRepository repository:
				_unitOfWork.CorrespondentRepo.Returns(repository);
				_unitOfWork.CorrespondentGroupRepo.Returns((ICorrespondentGroupRepository)_groupRepository);
				break;
			case IProjectRepository repository:
				_unitOfWork.ProjectRepo.Returns(repository);
				_unitOfWork.ProjectGroupRepo.Returns((IProjectGroupRepository)_groupRepository);
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
		_group = new TGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_param = new ElementParam
		{
			GroupId = _group.Id,
			Name = "New element",
			Description = "Description",
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
	public async Task Add_ValidInput_PersistsFieldsAndReturnsGeneratedId()
	{
		Guid id = await _service.Add(_param);
		DateTime now = _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;
		Assert.That(id, Is.Not.EqualTo(Guid.Empty));
		_repository.Received(1).Add(Arg.Is<TElement>(element =>
			element.Id == id && element.GroupId == _group.Id && ReferenceEquals(element.Group, _group) &&
			element.Name == _param.Name && element.Description == _param.Description &&
			element.IsFavorite && !element.IsDeleted && element.Order == 1 &&
			element.Original == now && element.Current == now));
		await _groupRepository.Received(1).GetWithContentsByIdAsync(_group.Id);
		await _unitOfWork.Received(1).SaveChangesAsync();
		_groupRepository.DidNotReceive().Update(Arg.Any<TGroup>());
	}

	[Test]
	public async Task Add_OptionalValues_PersistsNullAndFalse()
	{
		_param.Description = null;
		_param.IsFavorite = false;
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TElement>(element =>
			element.Description == null && !element.IsFavorite));
	}

	[Test]
	public async Task Add_ExistingElements_AppendsAfterMaximumIncludingDeleted()
	{
		TElement first = new() { Id = Guid.NewGuid(), Name = "First", Order = 2 };
		TElement deleted = new() { Id = Guid.NewGuid(), Name = "Deleted", Order = 9, IsDeleted = true };
		_group.Elements = [first, deleted];
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TElement>(element => element.Order == 10));
		Assert.That(first.Order, Is.EqualTo(2));
		Assert.That(deleted.Order, Is.EqualTo(9));
		_repository.DidNotReceive().Update(Arg.Any<TElement>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_DuplicateElementName_RejectsIncludingDeleted(bool deleted)
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = deleted });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_GroupAndChildWithSameName_DoesNotConflict()
	{
		_group.Name = _param.Name;
		_group.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _param.Name, Order = 100 });
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TElement>(element => element.Name == _param.Name && element.Order == 1));
	}

	[Test]
	public async Task Add_DifferentCase_UsesExactNameComparison()
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = _param.Name.ToUpperInvariant() });
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TElement>(element => element.Name == _param.Name));
	}

	[Test]
	public async Task Add_ToRoot_AllowsSelfParentGroup()
	{
		_group.ParentId = _group.Id;
		_group.Parent = _group;
		_group.Children.Add(_group);
		await _service.Add(_param);
		_repository.Received(1).Add(Arg.Is<TElement>(element => element.GroupId == _group.Id));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Add_InvalidName_RejectsBeforeLookup(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_NullInput_RejectsBeforeLookup()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(null!));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_EmptyGroupId_RejectsBeforeLookup()
	{
		_param.GroupId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Add_MissingGroup_RejectsWithoutSaving()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_DeletedGroup_RejectsWithoutSaving()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_OrderOverflow_RejectsWithoutSaving()
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "Last", Order = int.MaxValue });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_LookupFails_PropagatesWithoutWrites()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
		AssertNoWrites();
	}

	[Test]
	public void Add_RepositoryFails_DoesNotSave()
	{
		_repository.When(repository => repository.Add(Arg.Any<TElement>()))
			.Do(_ => throw new InvalidOperationException("Add failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
		AssertNoSave();
	}

	[Test]
	public void Add_SaveFails_PropagatesFailure()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("Save failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Add(_param));
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TElement>());
		_repository.DidNotReceive().Update(Arg.Any<TElement>());
		_groupRepository.DidNotReceive().Update(Arg.Any<TGroup>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
