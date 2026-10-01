using Business.Contracts.Services.Transactions;
using Business.Contracts.Params;
using Business.Contracts.Services.Base;

namespace Business.Contracts.Services;

public interface ITransactionService : IEntityService<TransactionParam>
{
	public Task DeleteTransactionList(List<Guid> transactionIds);
	/// <summary>
	/// Returns transactions in the selected range for the transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactions(DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Returns transactions having an entry using the selected account for the filtered transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByAccount(Guid accountId, DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected category for the filtered transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByCategory(Guid categoryId, DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected correspondent for the filtered transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByCorrespondent(Guid correspondentId, DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Returns transactions having an entry whose account references the selected project for the filtered transaction screen.
	/// Requires both calendar dates and includes both days in full in the current device timezone.
	/// Throws for a reversed range. Filters in the database and returns whole transactions with entries in Position order.
	/// Version 1 has no pagination; this read does not call AcceptChanges.
	/// </summary>
	public Task<List<TransactionInfo>> GetTransactionsByProject(Guid projectId, DateOnly fromDate, DateOnly toDate);

	/// <summary>
	/// Prepares an unsaved copy for the transaction editor, preserving accounts, amounts, rates, description and entry order.
	/// Sets DateTime to now; only the normal Add operation persists the edited copy.
	/// </summary>
	public Task<DuplicateTransactionInfo> DuplicateTransaction(Guid transactionId);


	/// <summary>
	/// Calculates balances for all accounts for the Accounts screen through the complete selected device-local day.
	/// Includes only Confirmed transactions and returns account-currency and base-currency totals.
	/// Base totals sum per-entry rounded amounts at stored rates; this read does not save changes.
	/// </summary>
	public Task<List<AccountBalanceInfo>> GetBalancesForAllAccounts(DateOnly date);

	/// <summary>
	/// Calculates one account balance through the complete selected device-local day for account display.
	/// Uses only Confirmed transactions and returns account-currency and base-currency totals at stored entry rates.
	/// Throws when the account does not exist; an existing account without qualifying entries has zero totals.
	/// </summary>
	public Task<AccountBalanceInfo> GetBalanceForAccount(Guid accountId, DateOnly date);
}
