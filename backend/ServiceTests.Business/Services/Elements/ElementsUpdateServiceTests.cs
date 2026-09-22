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
public sealed class ElementsUpdateServiceTests<TGroup, TElement, TService, TRepository, TGroupRepository>
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
	private TElement _element = null!;

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
		_element = new TElement
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Existing", Description = "Original description", Order = 1,
			Original = new DateTime(2020, 1, 1), Current = new DateTime(2020, 1, 2)
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
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
	public async Task Update_ValidInput_PersistsEditableFieldsOnly()
	{
		await _service.Update(_element.Id, _param);
		Assert.Multiple(() =>
		{
			Assert.That(_element.Name, Is.EqualTo(_param.Name));
			Assert.That(_element.Description, Is.EqualTo(_param.Description));
			Assert.That(_element.IsFavorite, Is.True);
			Assert.That(_element.IsDeleted, Is.False);
			Assert.That(_element.Order, Is.EqualTo(1));
			Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
			Assert.That(_element.Group, Is.SameAs(_group));
			Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(_element.Current, Is.EqualTo(Now));
		});
		_repository.Received(1).Update(_element);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task Update_UnchangedNameAndOptionalValues_AllowsSelf()
	{
		_param.Name = _element.Name;
		_param.Description = null;
		_param.IsFavorite = false;
		await _service.Update(_element.Id, _param);
		Assert.That(_element.Description, Is.Null);
		Assert.That(_element.IsFavorite, Is.False);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_DuplicateName_RejectsIncludingDeleted(bool deleted)
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = _param.Name, IsDeleted = deleted });
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_NameComparison_IsOrdinalAndIgnoresGroupNames()
	{
		_group.Name = _param.Name;
		_group.Children.Add(new TGroup { Id = Guid.NewGuid(), Name = _param.Name });
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = _param.Name.ToUpperInvariant() });
		await _service.Update(_element.Id, _param);
		Assert.That(_element.Name, Is.EqualTo(_param.Name));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void Update_InvalidName_RejectsBeforeReading(string? name)
	{
		_param.Name = name!;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Update_NullParam_Rejects()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, null!));
		AssertNoWrites();
	}

	[Test]
	public void Update_EmptyGroupId_Rejects()
	{
		_param.GroupId = Guid.Empty;
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_ChangedGroup_RejectsWithoutMoving()
	{
		_param.GroupId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, _param));
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void Update_MissingGroup_Rejects()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((TGroup?)null);
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_DeletedGroup_Rejects()
	{
		_group.IsDeleted = true;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_EmptyId_RejectsBeforeReading()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(Guid.Empty, _param));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_MissingOrDeletedElement_Rejects(bool deleted)
	{
		if (deleted)
		{
			_element.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns((TElement?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_ReadFailure_PropagatesWithoutWrites()
	{
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_GroupReadFailure_PropagatesWithoutWrites()
	{
		_groupRepository.GetWithContentsByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoWrites();
	}

	[Test]
	public void Update_UpdateFailure_DoesNotSave()
	{
		_element.Order = 5;
		_repository.When(repository => repository.Update(Arg.Any<TElement>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
		AssertNoSave();
	}

	[Test]
	public void Update_SaveFailure_Propagates()
	{
		_element.Order = 5;
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Update(_element.Id, _param));
	}

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

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
