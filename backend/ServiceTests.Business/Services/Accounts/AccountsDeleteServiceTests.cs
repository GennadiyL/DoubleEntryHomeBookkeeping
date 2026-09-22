using Business.Contracts.Params;
using Business.Contracts.Services;
using Business.Impl;
using Business.Models.Entities;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace ServiceTests.Business.Services.Accounts;

[TestFixture(Category = "Local")]
public sealed class AccountsDeleteServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAccountService _service = null!;
	private IAccountRepository _repository = null!;
	private IAccountGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private AccountGroup _group = null!;
	private Account _element = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IAccountRepository>();
		_groupRepository = Substitute.For<IAccountGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_unitOfWork.AccountRepo.Returns(_repository);
		_unitOfWork.AccountGroupRepo.Returns(_groupRepository);
		IServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddSharedMockModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unitOfWork);
		_provider = services.BuildServiceProvider(validateScopes: true);
		_scope = _provider.CreateScope();
		_service = _scope.ServiceProvider.GetRequiredService<IAccountService>();
		_group = new AccountGroup { Id = Guid.NewGuid(), Name = "Group" };
		_groupRepository.GetWithContentsByIdAsync(_group.Id).Returns(_group);
		_element = new Account
		{
			Id = Guid.NewGuid(), GroupId = _group.Id, Group = _group,
			Name = "Existing", Description = "Original description", Order = 1,
			Original = new DateTime(2020, 1, 1), Current = new DateTime(2020, 1, 2)
		};
		_group.Elements.Add(_element);
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns(_element);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Test]
	public async Task Delete_ActiveElement_SoftDeletesAndPreservesFields()
	{
		await _service.Delete(_element.Id);
		Assert.Multiple(() =>
		{
			Assert.That(_element.IsDeleted, Is.True);
			Assert.That(_element.Current, Is.EqualTo(Now));
			Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(_element.Name, Is.EqualTo("Existing"));
			Assert.That(_element.Description, Is.EqualTo("Original description"));
			Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
			Assert.That(_element.Order, Is.EqualTo(1));
			Assert.That(_element.IsFavorite, Is.False);
		});
		_repository.Received(1).Update(_element);
		await _unitOfWork.Received(1).SaveChangesAsync();
		Assert.That(_groupRepository.ReceivedCalls(), Is.Empty);
	}

	[Test]
	public void Delete_EmptyId_RejectsBeforeReading()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.Delete(Guid.Empty));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_MissingOrDeletedElement_Rejects(bool deleted)
	{
		if (deleted)
		{
			_element.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(_element.Id, CancellationToken.None).Returns((Account?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.Delete(_element.Id));
		AssertNoWrites();
	}

	[Test]
	public void Delete_ReadFailure_PropagatesWithoutWrites()
	{
		_repository.GetByIdAsync(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_element.Id));
		AssertNoWrites();
	}

	[Test]
	public void Delete_UpdateFailure_DoesNotSave()
	{
		_element.Order = 5;
		_repository.When(repository => repository.Update(Arg.Any<Account>()))
			.Do(_ => throw new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_element.Id));
		AssertNoSave();
	}

	[Test]
	public void Delete_SaveFailure_Propagates()
	{
		_element.Order = 5;
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.Delete(_element.Id));
	}

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<Account>());
		_repository.DidNotReceive().Update(Arg.Any<Account>());
		_groupRepository.DidNotReceive().Update(Arg.Any<AccountGroup>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
