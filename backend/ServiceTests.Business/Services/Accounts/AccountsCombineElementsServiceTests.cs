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
public sealed class AccountsCombineElementsServiceTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAccountService _service = null!;
	private IAccountRepository _repository = null!;
	private IAccountGroupRepository _groupRepository = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private AccountGroup _group = null!;
	private ITransactionEntryRepository _transactionRepository = null!;
	private ITemplateEntryRepository _templateRepository = null!;
	private List<TransactionEntry> _transactions = null!;
	private List<TemplateEntry> _templates = null!;
	private Account _destination = null!;
	private Account _element = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IAccountRepository>();
		_groupRepository = Substitute.For<IAccountGroupRepository>();
		_unitOfWork = Substitute.For<IAppUnitOfWork>();
		_transactionRepository = Substitute.For<ITransactionEntryRepository>();
		_templateRepository = Substitute.For<ITemplateEntryRepository>();
		_unitOfWork.TransactionEntryRepo.Returns(_transactionRepository);
		_unitOfWork.TemplateEntryRepo.Returns(_templateRepository);
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
		_element.CurrencyId = Guid.NewGuid();
		_destination = new Account
		{
			Id = Guid.NewGuid(), Name = "Destination", GroupId = Guid.NewGuid(),
			CurrencyId = _element.CurrencyId, Order = 4, CategoryId = Guid.NewGuid(),
			ProjectId = Guid.NewGuid(), CorrespondentId = Guid.NewGuid(),
			Original = new DateTime(2020, 2, 1), Current = new DateTime(2020, 2, 2)
		};
		_repository.GetByIdAsync(_destination.Id, CancellationToken.None).Returns(_destination);
		_transactions = [];
		_templates = [];
		_transactionRepository.GetByAccountIdAsync(_element.Id).Returns(_transactions);
		_templateRepository.GetByAccountIdAsync(_element.Id).Returns(_templates);
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CombineElements_References_RedirectsEntriesAndPreservesAmountsAndParents(bool deletedParents)
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), IsDeleted = deletedParents };
		Template template = new() { Id = Guid.NewGuid(), IsDeleted = deletedParents };
		TransactionEntry entry = new()
		{
			Id = Guid.NewGuid(), TransactionId = transaction.Id, Transaction = transaction,
			AccountId = _element.Id, Account = _element, Amount = -12.5m, Rate = 1.2m
		};
		TemplateEntry templateEntry = new()
		{
			Id = Guid.NewGuid(), TemplateId = template.Id, Template = template,
			AccountId = _element.Id, Account = _element, Amount = 20
		};
		_transactions.Add(entry);
		_templates.Add(templateEntry);
		Guid? categoryId = _destination.CategoryId;
		Guid? projectId = _destination.ProjectId;
		Guid? correspondentId = _destination.CorrespondentId;

		await _service.CombineElements(_destination.Id, _element.Id);

		Assert.Multiple(() =>
		{
			Assert.That(entry.AccountId, Is.EqualTo(_destination.Id));
			Assert.That(entry.Account, Is.SameAs(_destination));
			Assert.That(entry.Amount, Is.EqualTo(-12.5m));
			Assert.That(entry.Rate, Is.EqualTo(1.2m));
			Assert.That(entry.BaseAmount, Is.EqualTo(-15m));
			Assert.That(entry.TransactionId, Is.EqualTo(transaction.Id));
			Assert.That(entry.Transaction, Is.SameAs(transaction));
			Assert.That(templateEntry.AccountId, Is.EqualTo(_destination.Id));
			Assert.That(templateEntry.Account, Is.SameAs(_destination));
			Assert.That(templateEntry.Amount, Is.EqualTo(20));
			Assert.That(templateEntry.TemplateId, Is.EqualTo(template.Id));
			Assert.That(templateEntry.Template, Is.SameAs(template));
			Assert.That(_element.IsDeleted, Is.True);
			Assert.That(_element.Current, Is.EqualTo(Now));
			Assert.That(_element.Original, Is.EqualTo(new DateTime(2020, 1, 1)));
			Assert.That(_element.Name, Is.EqualTo("Existing"));
			Assert.That(_element.GroupId, Is.EqualTo(_group.Id));
			Assert.That(_element.CurrencyId, Is.EqualTo(_destination.CurrencyId));
			Assert.That(_destination.IsDeleted, Is.False);
			Assert.That(_destination.Current, Is.EqualTo(new DateTime(2020, 2, 2)));
			Assert.That(_destination.CategoryId, Is.EqualTo(categoryId));
			Assert.That(_destination.ProjectId, Is.EqualTo(projectId));
			Assert.That(_destination.CorrespondentId, Is.EqualTo(correspondentId));
		});
		_transactionRepository.Received(1).Update(entry);
		_templateRepository.Received(1).Update(templateEntry);
		_repository.Received(1).Update(_element);
		_repository.DidNotReceive().Update(_destination);
		await _transactionRepository.Received(1).GetByAccountIdAsync(_element.Id);
		await _templateRepository.Received(1).GetByAccountIdAsync(_element.Id);
		await _unitOfWork.Received(1).SaveChangesAsync();
		Received.InOrder(() =>
		{
			_transactionRepository.Update(entry);
			_templateRepository.Update(templateEntry);
			_repository.Update(_element);
			_unitOfWork.SaveChangesAsync();
		});
	}

	[Test]
	public async Task CombineElements_NoReferences_SoftDeletesSource()
	{
		await _service.CombineElements(_destination.Id, _element.Id);
		Assert.That(_element.IsDeleted, Is.True);
		_transactionRepository.DidNotReceive().Update(Arg.Any<TransactionEntry>());
		_templateRepository.DidNotReceive().Update(Arg.Any<TemplateEntry>());
		await _unitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public void CombineElements_DifferentCurrencies_RejectsBeforeReadingEntries()
	{
		_destination.CurrencyId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_element.IsDeleted, Is.False);
		Assert.That(_transactionRepository.ReceivedCalls(), Is.Empty);
		Assert.That(_templateRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void CombineElements_EmptyIdentifier_Rejects(bool source)
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.CombineElements(
			source ? _destination.Id : Guid.Empty, source ? Guid.Empty : _element.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void CombineElements_SameAccount_Rejects()
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await _service.CombineElements(_element.Id, _element.Id));
		Assert.That(_repository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void CombineElements_MissingOrDeletedAccount_Rejects(bool destination, bool deleted)
	{
		Account account = destination ? _destination : _element;
		if (deleted)
		{
			account.IsDeleted = true;
		}
		else
		{
			_repository.GetByIdAsync(account.Id, CancellationToken.None).Returns((Account?)null);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(_transactionRepository.ReceivedCalls(), Is.Empty);
		Assert.That(_templateRepository.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase("source")]
	[TestCase("destination")]
	[TestCase("transaction")]
	[TestCase("template")]
	public void CombineElements_ReadFailure_DoesNotMutateOrWrite(string stage)
	{
		TransactionEntry entry = NewTransactionEntry();
		_transactions.Add(entry);
		switch (stage)
		{
			case "source":
				_repository.GetByIdAsync(_element.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
				break;
			case "destination":
				_repository.GetByIdAsync(_destination.Id, CancellationToken.None).ThrowsAsync(new InvalidOperationException());
				break;
			case "transaction":
				_transactionRepository.GetByAccountIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
				break;
			case "template":
				_templateRepository.GetByAccountIdAsync(_element.Id).ThrowsAsync(new InvalidOperationException());
				break;
		}
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		Assert.That(entry.AccountId, Is.EqualTo(_element.Id));
		Assert.That(_element.IsDeleted, Is.False);
		AssertNoWrites();
	}

	[TestCase("transaction")]
	[TestCase("template")]
	[TestCase("account")]
	public void CombineElements_UpdateFailure_DoesNotSave(string stage)
	{
		_transactions.Add(NewTransactionEntry());
		_templates.Add(new TemplateEntry { Id = Guid.NewGuid(), Template = new Template(), Account = _element, AccountId = _element.Id });
		switch (stage)
		{
			case "transaction":
				_transactionRepository.When(repository => repository.Update(Arg.Any<TransactionEntry>()))
					.Do(_ => throw new InvalidOperationException());
				break;
			case "template":
				_templateRepository.When(repository => repository.Update(Arg.Any<TemplateEntry>()))
					.Do(_ => throw new InvalidOperationException());
				break;
			case "account":
				_repository.When(repository => repository.Update(Arg.Any<Account>()))
					.Do(_ => throw new InvalidOperationException());
				break;
		}
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
		AssertNoSave();
	}

	[Test]
	public void CombineElements_SaveFailure_Propagates()
	{
		_unitOfWork.SaveChangesAsync().ThrowsAsync(new InvalidOperationException());
		Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CombineElements(_destination.Id, _element.Id));
	}

	private TransactionEntry NewTransactionEntry() =>
		new() { Id = Guid.NewGuid(), Transaction = new Transaction(), Account = _element, AccountId = _element.Id, Amount = 10, Rate = 1 };

	private DateTime Now => _scope.ServiceProvider.GetRequiredService<IDateTimeService>().UtcNow;

	private void AssertNoWrites()
	{
		_repository.DidNotReceive().Add(Arg.Any<Account>());
		_repository.DidNotReceive().Update(Arg.Any<Account>());
		_transactionRepository.DidNotReceive().Update(Arg.Any<TransactionEntry>());
		_templateRepository.DidNotReceive().Update(Arg.Any<TemplateEntry>());
		AssertNoSave();
	}

	private void AssertNoSave() =>
		Assert.That(_unitOfWork.ReceivedCalls().Any(call =>
			call.GetMethodInfo().Name == nameof(IAppUnitOfWork.SaveChangesAsync)), Is.False);
}
