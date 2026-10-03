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
public sealed class GroupsGetByIdServiceTests<TGroup, TElement, TService, TRepository>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : ElementEntity<TGroup, TElement>, new()
	where TService : class, IReadEntityService<GroupInfo>
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
		_repository.GetById(_parent.Id, CancellationToken.None).Returns(_parent);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("Notes")]
	public async Task GetById_ExistingGroup_ReturnsDetachedFieldsWithoutSaving(string? description)
	{
		_group.Description = description;
		_group.IsFavorite = true;
		_group.ModificationType = ModificationType.Content | ModificationType.Order;
		_group.Parent = null!;
		GroupInfo result = await _service.GetById(_group.Id);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(_group.Id));
			Assert.That(result.ParentId, Is.EqualTo(_parent.Id));
			Assert.That(result.ParentName, Is.EqualTo(_parent.Name));
			Assert.That(result.Name, Is.EqualTo(_group.Name));
			Assert.That(result.Description, Is.EqualTo(description));
			Assert.That(result.Order, Is.EqualTo(7));
			Assert.That(result.IsFavorite, Is.True);
			Assert.That(result.IsRoot, Is.False);
		});
		result.Name = "DTO edit";
		Assert.That(_group.Name, Is.EqualTo("Old name"));
		Assert.That(_group.EditRevision, Is.EqualTo(1));
		Assert.That(_group.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		AssertNoWrites();
	}

	[Test]
	public async Task GetById_Root_ReturnsSelfParentAndRootIndicator()
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
		_group.ParentId = _group.Id;
		_repository.GetById(_group.Id, CancellationToken.None).Returns(_group);
		GroupInfo result = await _service.GetById(_group.Id);
		Assert.That(result.IsRoot, Is.True);
		Assert.That(result.ParentId, Is.EqualTo(_group.Id));
		Assert.That(result.ParentName, Is.EqualTo(_group.Name));
		await _repository.Received(1).GetById(_group.Id, CancellationToken.None);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_UnknownOrEmptyId_ThrowsNotFound(bool empty)
	{
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetById(empty ? Guid.Empty : Guid.NewGuid()));
		AssertNoWrites();
	}

	[TestCase(0L)]
	[TestCase(7L)]
	public void GetById_DeletedGroup_ThrowsNotFound(long revision)
	{
		_group.DeleteRevision = revision;
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetById(_group.Id));
		Assert.That(_group.DeleteRevision, Is.EqualTo(revision));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_InvalidParent_ThrowsNotFound(bool deleted)
	{
		if (deleted) { _parent.DeleteRevision = 0; }
		else { _repository.GetById(_parent.Id, CancellationToken.None).Returns((TGroup?)null); }
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await _service.GetById(_group.Id));
		AssertNoWrites();
	}

	[Test]
	public void GetById_LookupFailure_PropagatesWithoutSaving()
	{
		_repository.GetById(_group.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException("Read failed."));
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetById(_group.Id));
		AssertNoWrites();
	}

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<TGroup>());
		_repository.DidNotReceive().Update(Arg.Any<TGroup>());
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChanges)), Is.False);
	}
}
