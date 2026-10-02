using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies template FromTransaction through the public service contract.
/// Uses production service composition and substituted repositories.
/// Builds detached projections from explicitly loaded entries.
/// Checks copied values and persisted entry ordering.
/// Covers missing or deleted source data.
/// Confirms previews neither mutate nor save aggregates.
/// Forwards cancellation to the required repository reads.
/// Editor changes to results cannot alter their source values.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesFromTransactionServiceTests : TemplatesServiceTestsBase
{

	[TestCase(TransactionState.Draft)]
	[TestCase(TransactionState.Confirmed)]
	public async Task FromTransaction_CopiesDescriptionAccountsAmountsAndOrder(TransactionState state)
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), State = state, Description = "Transaction notes" };
		List<TransactionEntry> entries =
		[
			new TransactionEntry { Id = Guid.NewGuid(), Transaction = transaction, TransactionId = transaction.Id,
				Account = Account, AccountId = Account.Id, Amount = 7m, Rate = 3m, Position = 1 },
			new TransactionEntry { Id = Guid.NewGuid(), Transaction = transaction, TransactionId = transaction.Id,
				Account = Account, AccountId = Account.Id, Amount = -2m, Rate = 4m, Position = 0 }
		];
		UnitOfWork.TransactionRepo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
		UnitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(entries);

		using CancellationTokenSource source = new();
		FromTransactionInfo result = await Service.FromTransaction(transaction.Id, source.Token);

		Assert.That(result.Description, Is.EqualTo(transaction.Description));
		Assert.That(result.Entries.Select(entry => entry.Amount), Is.EqualTo(new[] { -2m, 7m }));
		Assert.That(result.Entries.All(entry => entry.AccountName == Account.Name && entry.CurrencyName == Currency.Name), Is.True);
		result.Entries[0].Amount = 100m;
		Assert.That(entries[1].Amount, Is.EqualTo(-2m));
		await UnitOfWork.TransactionEntryRepo.Received(1).GetByTransactionIdAsync(transaction.Id, source.Token);
		AssertNoWrites();
	}

	[TestCase(TransactionState.Undefined)]
	[TestCase(TransactionState.Planned)]
	public void FromTransaction_RejectsUnsupportedState(TransactionState state)
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), State = state };
		UnitOfWork.TransactionRepo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.FromTransaction(transaction.Id));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FromTransaction_RejectsMissingOrDeletedTransaction(bool deleted)
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), State = TransactionState.Draft, DeleteRevision = 0 };
		if (deleted)
		{
			UnitOfWork.TransactionRepo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
		}
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.FromTransaction(transaction.Id));
		AssertNoWrites();
	}

	[Test]
	public async Task FromTransaction_AllowsEmptyDraft()
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), State = TransactionState.Draft };
		UnitOfWork.TransactionRepo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
		UnitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(new List<TransactionEntry>());
		FromTransactionInfo result = await Service.FromTransaction(transaction.Id);
		Assert.That(result.Entries, Is.Empty);
		AssertNoWrites();
	}
}