using Business.Models.Enums;
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
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;
using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;

namespace ServiceTests.Business.Services.Elements;

/// <summary>
/// Verifies shared classification lifecycle rules against the TRD.
/// Runs through Category, Correspondent and Project service interfaces.
/// Resolves production services through dependency injection with repository substitutes.
/// Covers creation tracking, trimmed names and invalid references.
/// Checks soft deletion, retained revisions and sibling normalization.
/// Verifies movement and merge tracking with one persistence boundary.
/// Checks detached editor reads and cancellation forwarding.
/// Keeps account and template-specific service behavior outside this fixture.
/// </summary>
[TestFixture(typeof(CategoryGroup), typeof(Category), typeof(ICategoryService), typeof(ICategoryRepository), typeof(ICategoryGroupRepository), Category = "Local")]
[TestFixture(typeof(CorrespondentGroup), typeof(Correspondent), typeof(ICorrespondentService), typeof(ICorrespondentRepository), typeof(ICorrespondentGroupRepository), Category = "Local")]
[TestFixture(typeof(ProjectGroup), typeof(Project), typeof(IProjectService), typeof(IProjectRepository), typeof(IProjectGroupRepository), Category = "Local")]
public sealed class ElementsLifecycleServiceTests<TGroup, TElement, TService, TRepository, TGroupRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IElementService<TGroup, TElement>, IUpdateEntityService<ElementParam>, IReadEntityService<ElementInfo>
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
	private TElement _element = null!;
	private IAccountRepository _accountRepository = null!;
	private List<Account> _accounts = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<TRepository>();
		_groupRepository = Substitute.For<TGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_accountRepository = Substitute.For<IAccountRepository>();
		_unitOfWork.AccountRepo.Returns(_accountRepository);
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
			EditRevision = 1, ModificationType = ModificationType.None
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
		_accounts = [];
		_accountRepository.GetByCategoryIdAsync(_element.Id, Arg.Any<CancellationToken>()).Returns(_accounts);
		_accountRepository.GetByCorrespondentIdAsync(_element.Id, Arg.Any<CancellationToken>()).Returns(_accounts);
		_accountRepository.GetByProjectIdAsync(_element.Id, Arg.Any<CancellationToken>()).Returns(_accounts);
		_groupRepository.GetByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(_group);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Save_TrimsNameAndPreservesDescription(bool update)
	{
		ElementParam input = new() { GroupId = _group.Id, Name = "  Trimmed  ", Description = "  Notes  " };
		if (update)
		{
			_element.ModificationType = ModificationType.Order;
			await _service.Update(_element.Id, input);
			Assert.That(_element.Name, Is.EqualTo("Trimmed"));
			Assert.That(_element.Description, Is.EqualTo("  Notes  "));
			Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
			Assert.That(_element.EditRevision, Is.EqualTo(1));
		}
		else
		{
			await _service.Add(input);
			_repository.Received(1).Add(Arg.Is<TElement>(item => item.Name == "Trimmed" &&
				item.Description == "  Notes  " && item.EditRevision == null &&
				item.DeleteRevision == null && item.ModificationType == ModificationType.None));
		}
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Save_TrimmedCaseInsensitiveCollision_Rejects(bool update)
	{
		_group.Elements.Add(new TElement { Id = Guid.NewGuid(), Name = "  COLLISION  ", DeleteRevision = 0 });
		ElementParam input = new() { GroupId = _group.Id, Name = " collision " };
		if (update)
		{
			Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Update(_element.Id, input));
		}
		else
		{
			Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Add(input));
		}
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_ReferencedByAccount_RejectsWithoutChanges(bool deleted)
	{
		_accounts.Add(new Account { Id = Guid.NewGuid(), DeleteRevision = deleted ? 0 : null });

		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Delete(_element.Id));

		Assert.That(_element.DeleteRevision, Is.Null);
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[TestCase(null)]
	[TestCase(0L)]
	[TestCase(7L)]
	public async Task Delete_PreservesRevisionAndFlags_NormalizesOnlyLiveSurvivors(long? revision)
	{
		_element.EditRevision = revision;
		_element.ModificationType = ModificationType.Order;
		TElement survivor = new() { Id = Guid.NewGuid(), Name = "Survivor", Order = 9,
			EditRevision = 5, ModificationType = ModificationType.Content };
		TElement deleted = new() { Id = Guid.NewGuid(), Order = 4, DeleteRevision = 0 };
		_group.Elements.Add(survivor);
		_group.Elements.Add(deleted);

		await _service.Delete(_element.Id);

		Assert.That(_element.EditRevision, Is.EqualTo(revision));
		Assert.That(_element.DeleteRevision, Is.Zero);
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Order));
		Assert.That(survivor.Order, Is.Zero);
		Assert.That(survivor.EditRevision, Is.EqualTo(5));
		Assert.That(survivor.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(deleted.Order, Is.EqualTo(4));
		_repository.Received(1).Update(survivor);
		_repository.DidNotReceive().Update(deleted);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_InvalidContainingGroup_DoesNotDelete(bool deleted)
	{
		if (deleted) { _group.DeleteRevision = 0; }
		else { _groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((TGroup?)null); }

		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.Delete(_element.Id));

		Assert.That(_element.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[Test]
	public async Task Move_NormalizesBothCollectionsAndPreservesContentFlags()
	{
		TElement sourceSibling = new() { Id = Guid.NewGuid(), Order = 8, ModificationType = ModificationType.Content };
		_group.Elements.Add(sourceSibling);
		TElement targetSibling = new() { Id = Guid.NewGuid(), Name = "Other", Order = 9, ModificationType = ModificationType.Content };
		TElement deleted = new() { Id = Guid.NewGuid(), Name = "Deleted", Order = 4, DeleteRevision = 0 };
		TGroup destination = new() { Id = Guid.NewGuid(), Elements = [targetSibling, deleted] };
		_groupRepository.GetWithContentsByIdAsync(destination.Id).Returns(destination);

		await _service.MoveToAnotherGroup(_element.Id, destination.Id);

		Assert.That(sourceSibling.Order, Is.Zero);
		Assert.That(targetSibling.Order, Is.Zero);
		Assert.That(_element.Order, Is.EqualTo(1));
		Assert.That(_element.EditRevision, Is.EqualTo(1));
		Assert.That(_element.GroupId, Is.EqualTo(destination.Id));
		foreach (TElement item in new[] { sourceSibling, targetSibling, _element })
		{
			Assert.That(item.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
			_repository.Received(1).Update(item);
		}
		Assert.That(deleted.Order, Is.EqualTo(4));
		_repository.DidNotReceive().Update(deleted);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void Move_MissingSourceGroup_DoesNotModifyDestination()
	{
		TElement sibling = new() { Id = Guid.NewGuid(), Name = "Other", Order = 8 };
		TGroup destination = new() { Id = Guid.NewGuid(), Elements = [sibling] };
		_groupRepository.GetWithContentsByIdAsync(destination.Id).Returns(destination);
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns((TGroup?)null);

		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.MoveToAnotherGroup(_element.Id, destination.Id));

		Assert.That(sibling.Order, Is.EqualTo(8));
		Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Combine_EqualIds_ReturnsWithoutLookups(bool empty)
	{
		Guid id = empty ? Guid.Empty : _element.Id;

		await _service.CombineElements(id, id);

		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		Assert.That(_accountRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public async Task Combine_SameGroup_NormalizesDestinationAndTracksSourceDeletion()
	{
		TElement destination = new() { Id = Guid.NewGuid(), Name = "Destination", GroupId = _group.Id,
			Order = 8, EditRevision = 7, ModificationType = ModificationType.Content };
		_group.Elements.Add(destination);
		_repository.GetByIdAsync(destination.Id).Returns(destination);
		_element.EditRevision = null;
		_element.ModificationType = ModificationType.Order;

		await _service.CombineElements(destination.Id, _element.Id);

		Assert.That(_element.EditRevision, Is.Null);
		Assert.That(_element.DeleteRevision, Is.Zero);
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(destination.Order, Is.Zero);
		Assert.That(destination.EditRevision, Is.EqualTo(7));
		Assert.That(destination.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		_repository.Received(1).Update(destination);
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("Notes")]
	public async Task GetById_ReturnsDetachedValuesAndGroupLabel(string? description)
	{
		_element.Description = description;
		_element.IsFavorite = true;
		_element.ModificationType = ModificationType.Content | ModificationType.Order;
		_element.Group = null!;

		ElementInfo info = await _service.GetById(_element.Id);

		Assert.Multiple(() =>
		{
			Assert.That(info.Id, Is.EqualTo(_element.Id));
			Assert.That(info.GroupId, Is.EqualTo(_group.Id));
			Assert.That(info.GroupName, Is.EqualTo(_group.Name));
			Assert.That(info.Name, Is.EqualTo(_element.Name));
			Assert.That(info.Description, Is.EqualTo(description));
			Assert.That(info.IsFavorite, Is.True);
			Assert.That(info.Order, Is.EqualTo(_element.Order));
		});
		info.Name = "Changed DTO";
		Assert.That(_element.Name, Is.EqualTo("Existing"));
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_UnknownOrEmpty_ThrowsNotFound(bool empty)
	{
		Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			await _service.GetById(empty ? Guid.Empty : Guid.NewGuid()));
		AssertNoWrites();
	}

	[TestCase(0L)]
	[TestCase(5L)]
	public void GetById_Deleted_ThrowsNotFound(long revision)
	{
		_element.DeleteRevision = revision;

		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.GetById(_element.Id));

		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_InvalidGroup_ThrowsNotFound(bool deleted)
	{
		if (deleted) { _group.DeleteRevision = 0; }
		else { _groupRepository.GetByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns((TGroup?)null); }

		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetById(_element.Id));

		AssertNoWrites();
	}

	[Test]
	public async Task GetById_ForwardsCancellationToBothLookups()
	{
		using CancellationTokenSource cancellation = new();
		CancellationToken token = cancellation.Token;
		_repository.GetByIdAsync(_element.Id, token).Returns(_element);

		await _service.GetById(_element.Id, token);

		await _repository.Received(1).GetByIdAsync(_element.Id, token);
		await _groupRepository.Received(1).GetByIdAsync(_group.Id, token);
		AssertNoWrites();
	}

	[Test]
	public void GetById_CancelledLookup_PropagatesWithoutWrites()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		_repository.GetByIdAsync(_element.Id, cancellation.Token).ThrowsAsync(new OperationCanceledException(cancellation.Token));

		Assert.ThrowsAsync<OperationCanceledException>(async () => await _service.GetById(_element.Id, cancellation.Token));

		AssertNoWrites();
	}

	[Test]
	public async Task Add_ForwardsCancellationToLookupAndCommit()
	{
		using CancellationTokenSource cancellation = new();
		CancellationToken token = cancellation.Token;
		_groupRepository.GetWithContentsByIdAsync(_group.Id, token).Returns(_group);

		await _service.Add(new ElementParam { GroupId = _group.Id, Name = "New" }, token);

		await _groupRepository.Received(1).GetWithContentsByIdAsync(_group.Id, token);
		await _unitOfWork.Received(1).SaveChangesAsync(token);
	}

	[TestCase(null)]
	[TestCase(0L)]
	[TestCase(9L)]
	public async Task SetFavoriteStatus_PreservesOrderFlagAndRevision(long? revision)
	{
		_element.EditRevision = revision;
		_element.ModificationType = ModificationType.Order;

		await _service.SetFavoriteStatus(_element.Id, true);

		Assert.That(_element.EditRevision, Is.EqualTo(revision));
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(_element.Order, Is.EqualTo(1));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task SetOrder_PreservesContentAndRevision()
	{
		_element.ModificationType = ModificationType.Content;
		_element.EditRevision = 7;

		await _service.SetOrder(_element.Id, 0);

		Assert.That(_element.Order, Is.Zero);
		Assert.That(_element.EditRevision, Is.EqualTo(7));
		Assert.That(_element.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task Delete_TiedSurvivors_UsesCanonicalGuidOrder()
	{
		TElement first = new() { Id = Guid.Parse("00000001-0000-0000-0000-000000000000"), Order = 8 };
		TElement second = new() { Id = Guid.Parse("01000000-0000-0000-0000-000000000000"), Order = 8 };
		_group.Elements.Add(second);
		_group.Elements.Add(first);

		await _service.Delete(_element.Id);

		Assert.That(first.Order, Is.Zero);
		Assert.That(second.Order, Is.EqualTo(1));
	}

	[Test]
	public async Task Delete_ForwardsCancellationToReferenceLookupAndCommit()
	{
		using CancellationTokenSource cancellation = new();
		CancellationToken token = cancellation.Token;
		_repository.GetByIdAsync(_element.Id, token).Returns(_element);
		_groupRepository.GetWithContentsByIdAsync(_group.Id, token).Returns(_group);

		await _service.Delete(_element.Id, token);

		await _repository.Received(1).GetByIdAsync(_element.Id, token);
		await _groupRepository.Received(1).GetWithContentsByIdAsync(_group.Id, token);
		Assert.That(_accountRepository.ReceivedCalls().Single().GetArguments()[1], Is.EqualTo(token));
		await _unitOfWork.Received(1).SaveChangesAsync(token);
	}

	[Test]
	public void Delete_ReferenceLookupFailure_DoesNotDelete()
	{
		_accountRepository.GetByCategoryIdAsync(_element.Id, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());
		_accountRepository.GetByCorrespondentIdAsync(_element.Id, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());
		_accountRepository.GetByProjectIdAsync(_element.Id, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());

		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_element.Id));

		Assert.That(_element.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_LookupFailure_PropagatesWithoutWrites(bool group)
	{
		if (group) { _groupRepository.GetByIdAsync(_group.Id).ThrowsAsync(new InvalidOperationException()); }
		else { _repository.GetByIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException()); }

		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetById(_element.Id));

		AssertNoWrites();
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TElement>());
		_repository.DidNotReceive().Update(Arg.Any<TElement>());
		_accountRepository.DidNotReceive().Update(Arg.Any<Account>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
	}
}
