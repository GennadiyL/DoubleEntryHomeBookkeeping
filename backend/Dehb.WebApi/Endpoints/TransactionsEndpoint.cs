using Business.Contracts.Services;
using Business.Contracts.Services.Transactions;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

/// <summary>
/// Exposes transaction operations through the HTTP boundary.
/// Delegates validation and persistence to the Business service.
/// Reads return complete transaction projections and balance results.
/// Commands accept JSON editor or deletion parameters.
/// Refresh accepts a body to accommodate 300 displayed identities.
/// Refresh remains read-only despite using POST for transport.
/// Cancellation follows the incoming HTTP request.
/// Errors use the centralized exception handler.
/// </summary>
internal static class TransactionsEndpoint
{
	public static async Task<IResult> GetByIdHandler(Guid id, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> AddHandler(TransactionParam param, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> UpdateHandler(UpdateTransactionParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> GetTransactionsHandler(DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTransactions(date, cancellationToken));

	public static async Task<IResult> DeleteTransactionsHandler(DeleteTransactionsParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.DeleteTransactions(param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> RefreshTransactionsHandler(TransactionRefreshParam param, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.RefreshTransactions(param, cancellationToken));

	public static async Task<IResult> DuplicateTransactionHandler(Guid transactionId, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.DuplicateTransaction(transactionId, cancellationToken));

	public static async Task<IResult> GetBalancesForAllAccountsHandler(DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetBalancesForAllAccounts(date, cancellationToken));

	public static async Task<IResult> GetBalanceForAccountHandler(Guid accountId, DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetBalanceForAccount(accountId, date, cancellationToken));
	public static async Task<IResult> GetTransactionsByAccountHandler(Guid accountId, DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTransactionsByAccount(accountId, date, cancellationToken));

	public static async Task<IResult> DeleteTransactionsByAccountHandler(DeleteTransactionsByAccountParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.DeleteTransactionsByAccount(param.AccountId, param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> GetTransactionsByCategoryHandler(Guid categoryId, DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTransactionsByCategory(categoryId, date, cancellationToken));

	public static async Task<IResult> DeleteTransactionsByCategoryHandler(DeleteTransactionsByCategoryParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.DeleteTransactionsByCategory(param.CategoryId, param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> GetTransactionsByCorrespondentHandler(Guid correspondentId, DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTransactionsByCorrespondent(correspondentId, date, cancellationToken));

	public static async Task<IResult> DeleteTransactionsByCorrespondentHandler(DeleteTransactionsByCorrespondentParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.DeleteTransactionsByCorrespondent(param.CorrespondentId, param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> GetTransactionsByProjectHandler(Guid projectId, DateOnly date, ITransactionService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTransactionsByProject(projectId, date, cancellationToken));

	public static async Task<IResult> DeleteTransactionsByProjectHandler(DeleteTransactionsByProjectParam param, ITransactionService service, CancellationToken cancellationToken = default)
	{
		await service.DeleteTransactionsByProject(param.ProjectId, param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

}
