using Business.Contracts.Services.Transactions;
using Business.Contracts.Base.Services;

namespace Business.Contracts.Services;

/// <summary>
/// Provides transaction editing, filtered reads, duplication and account balance calculations.
/// Editor mutations validate the aggregate and derive Draft or Confirmed state.
/// Updates preserve parent identity and replace all entries in submitted order.
/// Calendar filters include both endpoints in the current device timezone.
/// Reads return complete matching transactions rather than only their matching entries.
/// Bulk deletion applies the shared soft-delete lifecycle in one atomic action.
/// Only Confirmed transactions contribute to balances using stored entry rates.
/// Duplicate preparation and all reads are detached operations without persistence.
/// </summary>
public interface ITransactionService : IUpdateEntityService<TransactionParam>,
	IReadEntityService<TransactionInfo>
{
	/// <summary>
	/// Deletes matching Draft and Confirmed transactions for the transaction screen.
	/// Matches all transactions in the inclusive device-local date range, including empty Drafts.
	/// Includes both selected local days; a reversed range raises a validation exception.
	/// No matches completes successfully without a commit.
	/// All aggregate changes and sync tracking commit once or roll back together.
	/// A database failure raises a critical exception; local writes are not automatically retried.
	/// </summary>
	public Task DeleteTransactions(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes matching Draft and Confirmed transactions for the transaction screen.
	/// Matches when at least one entry belongs to the account; deletes the complete transaction.
	/// Includes both selected local days; a reversed range raises a validation exception.
	/// Unknown or deleted filter identities are invalid; no matches completes successfully without a commit.
	/// All aggregate changes and sync tracking commit once or roll back together.
	/// A database failure raises a critical exception; local writes are not automatically retried.
	/// </summary>
	public Task DeleteTransactionsByAccount(Guid accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes matching Draft and Confirmed transactions for the transaction screen.
	/// Matches when an entry account currently has the selected category; deletes the complete transaction.
	/// Includes both selected local days; a reversed range raises a validation exception.
	/// Unknown or deleted filter identities are invalid; no matches completes successfully without a commit.
	/// All aggregate changes and sync tracking commit once or roll back together.
	/// A database failure raises a critical exception; local writes are not automatically retried.
	/// </summary>
	public Task DeleteTransactionsByCategory(Guid categoryId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes matching Draft and Confirmed transactions for the transaction screen.
	/// Matches when an entry account currently has the selected correspondent; deletes the complete transaction.
	/// Includes both selected local days; a reversed range raises a validation exception.
	/// Unknown or deleted filter identities are invalid; no matches completes successfully without a commit.
	/// All aggregate changes and sync tracking commit once or roll back together.
	/// A database failure raises a critical exception; local writes are not automatically retried.
	/// </summary>
	public Task DeleteTransactionsByCorrespondent(Guid correspondentId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes matching Draft and Confirmed transactions for the transaction screen.
	/// Matches when an entry account currently has the selected project; deletes the complete transaction.
	/// Includes both selected local days; a reversed range raises a validation exception.
	/// Unknown or deleted filter identities are invalid; no matches completes successfully without a commit.
	/// All aggregate changes and sync tracking commit once or roll back together.
	/// A database failure raises a critical exception; local writes are not automatically retried.
	/// </summary>
	public Task DeleteTransactionsByProject(Guid projectId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns transactions in the selected range for the transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactions(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns transactions having an entry using the selected account for the filtered transaction screen.
	/// Throws for an unknown or deleted filter identity; a valid filter with no matches returns an empty list.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByAccount(Guid accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected category for the filtered transaction screen.
	/// Throws for an unknown or deleted filter identity; a valid filter with no matches returns an empty list.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByCategory(Guid categoryId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected correspondent for the filtered transaction screen.
	/// Throws for an unknown or deleted filter identity; a valid filter with no matches returns an empty list.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByCorrespondent(Guid correspondentId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected project for the filtered transaction screen.
	/// Throws for an unknown or deleted filter identity; a valid filter with no matches returns an empty list.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByProject(Guid projectId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

	/// <summary>
	/// Prepares an unsaved copy for the transaction editor, preserving accounts, amounts, rates, description and entry order.
	/// Sets DateTime to now; only the normal Add operation persists the edited copy.
	/// </summary>
	public Task<DuplicateTransactionInfo> DuplicateTransaction(Guid transactionId, CancellationToken cancellationToken = default);


	/// <summary>
	/// Calculates balances for all accounts for the Accounts screen through the complete selected device-local day.
	/// Includes only Confirmed transactions and returns account-currency and base-currency totals.
	/// Base totals sum per-entry rounded amounts at stored rates; this read does not save changes.
	/// </summary>
	public Task<List<AccountBalanceInfo>> GetBalancesForAllAccounts(DateOnly date, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calculates one account balance through the complete selected device-local day for account display.
	/// Uses only Confirmed transactions and returns account-currency and base-currency totals at stored entry rates.
	/// Throws when the account does not exist; an existing account without qualifying entries has zero totals.
	/// </summary>
	public Task<AccountBalanceInfo> GetBalanceForAccount(Guid accountId, DateOnly date, CancellationToken cancellationToken = default);
}
