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
using DataAccess.Core.Behaviors;
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
public sealed class GroupsGetTreeServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IGroupService<TGroup, TElement>
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
			EditRevision = 1,
			ModificationType = ModificationType.None
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
		_repository.GetWithChildrenByIdAsync(_parent.Id).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task GetTree_ReturnsDetachedGroupsAndElementsIncludingRootElements()
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
		_group.ParentId = _parent.Id;
		TElement rootElement = new() { Id = Guid.NewGuid(), GroupId = _parent.Id, Name = "Root element", Order = 4 };
		TElement element = new()
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Name = "Element", Description = "Notes",
			Order = 8, IsFavorite = true, EditRevision = 7,
			ModificationType = ModificationType.Content | ModificationType.Order
		};
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { _group, _parent });
		_elementRepository.GetAllAsync(CancellationToken.None).Returns(new List<TElement> { element, rootElement });
		TreeInfo result = await _service.GetTree();
		ElementInfo info = result.Elements.Single(item => item.Id == element.Id);
		Assert.Multiple(() =>
		{
			Assert.That(result.Groups.Count, Is.EqualTo(2));
			Assert.That(result.Groups.Count(item => item.IsRoot), Is.EqualTo(1));
			Assert.That(result.Elements.Count, Is.EqualTo(2));
			Assert.That(result.Elements[0].GroupId, Is.EqualTo(_parent.Id));
			Assert.That(result.Elements[0].GroupName, Is.EqualTo(_parent.Name));
			Assert.That(result.Elements[0].Description, Is.Null);
			Assert.That(result.Elements[0].IsFavorite, Is.False);
			Assert.That(info.GroupId, Is.EqualTo(_group.Id));
			Assert.That(info.GroupName, Is.EqualTo(_group.Name));
			Assert.That(info.Name, Is.EqualTo("Element"));
			Assert.That(info.Description, Is.EqualTo("Notes"));
			Assert.That(info.Order, Is.EqualTo(8));
			Assert.That(info.IsFavorite, Is.True);
			Assert.That(info.GetType(), Is.EqualTo(typeof(ElementInfo)));
		});
		info.Name = "Changed DTO";
		result.Groups.Single(item => item.Id == _group.Id).Name = "Changed group DTO";
		Assert.That(element.Name, Is.EqualTo("Element"));
		Assert.That(_group.Name, Is.EqualTo("Old name"));
		Assert.That(element.EditRevision, Is.EqualTo(7));
		Assert.That(element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		AssertReadOnly();
	}

	[Test]
	public async Task GetTree_DeletedGroupsAndElements_AreExcluded()
	{
		TGroup deletedGroup = new() { Id = Guid.NewGuid(), DeleteRevision = 0 };
		TElement pending = new() { Id = Guid.NewGuid(), GroupId = deletedGroup.Id, DeleteRevision = 0 };
		TElement deleted = new() { Id = Guid.NewGuid(), GroupId = _parent.Id, DeleteRevision = 7 };
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { _parent, deletedGroup });
		_elementRepository.GetAllAsync(CancellationToken.None).Returns(new List<TElement> { pending, deleted });
		TreeInfo result = await _service.GetTree();
		Assert.That(result.Groups.Select(item => item.Id), Is.EqualTo(new[] { _parent.Id }));
		Assert.That(result.Elements, Is.Empty);
		AssertReadOnly();
	}

	[Test]
	public async Task GetTree_EqualOrders_UsesCanonicalGuidTieBreakWithoutRenumbering()
	{
		TElement first = new() { Id = Guid.Parse("00000001-0000-0000-0000-000000000000"), GroupId = _parent.Id, Order = 9 };
		TElement second = new() { Id = Guid.Parse("80000000-0000-0000-0000-000000000000"), GroupId = _parent.Id, Order = 9 };
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { _parent });
		_elementRepository.GetAllAsync(CancellationToken.None).Returns(new List<TElement> { second, first });
		TreeInfo result = await _service.GetTree();
		Assert.That(result.Elements.Select(item => item.Id), Is.EqualTo(new[] { first.Id, second.Id }));
		Assert.That(result.Elements.All(item => item.Order == 9), Is.True);
		Assert.That(first.ModificationType, Is.EqualTo(ModificationType.None));
		AssertReadOnly();
	}

	[Test]
	public async Task GetTree_EmptyCatalog_ReturnsEmptyCollections()
	{
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup>());
		_elementRepository.GetAllAsync(CancellationToken.None).Returns(new List<TElement>());
		TreeInfo result = await _service.GetTree();
		Assert.That(result.Groups, Is.Empty);
		Assert.That(result.Elements, Is.Empty);
		AssertReadOnly();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetTree_LiveElementWithUnavailableGroup_ThrowsNotFound(bool deleted)
	{
		_parent.DeleteRevision = deleted ? 0 : null;
		_repository.GetAllAsync(CancellationToken.None).Returns(deleted ? new List<TGroup> { _parent } : new List<TGroup>());
		_elementRepository.GetAllAsync(CancellationToken.None).Returns(new List<TElement> { new() { Id = Guid.NewGuid(), GroupId = _parent.Id } });
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetTree());
		AssertReadOnly();
	}

	[Test]
	public void GetTree_ElementLookupFailure_PropagatesWithoutSaving()
	{
		_repository.GetAllAsync(CancellationToken.None).Returns(new List<TGroup> { _parent });
		_elementRepository.GetAllAsync(CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetTree());
		AssertReadOnly();
	}

	[Test]
	public async Task GetTree_CancellationToken_IsPassedToBothRepositories()
	{
		using CancellationTokenSource cancellation = new();
		CancellationToken token = cancellation.Token;
		_repository.GetAllAsync(token).Returns(new List<TGroup> { _parent });
		_elementRepository.GetAllAsync(token).Returns(new List<TElement>());
		TreeInfo result = await _service.GetTree(token);
		Assert.That(result.Groups.Count, Is.EqualTo(1));
		await _repository.Received(1).GetAllAsync(token);
		await _elementRepository.Received(1).GetAllAsync(token);
		AssertReadOnly();
	}

	private void AssertReadOnly()
	{
		Assert.That(_repository.ReceivedCalls().Count(), Is.EqualTo(1));
		Assert.That(_repository.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("GetAllAsync"));
		Assert.That(_elementRepository.ReceivedCalls().Count(), Is.EqualTo(1));
		Assert.That(_elementRepository.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("GetAllAsync"));
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
