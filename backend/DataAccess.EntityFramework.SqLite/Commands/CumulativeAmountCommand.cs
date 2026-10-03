using Business.Models.Enums;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.SqLite.Utils;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Rebuilds an account cache with one parameterized SQLite statement.
/// A seed row supplies the opening amount without persisting a fake entry.
/// The range includes every entry at the supplied timestamp.
/// Only live Confirmed entries contribute to the running sum.
/// Draft entries receive zero and deleted entries remain untouched.
/// Integer SUM rejects overflow instead of using floating-point totals.
/// The statement updates no synchronization metadata.
/// The service owns the transaction and handles execution failures.
/// </summary>
internal sealed class CumulativeAmountCommand : ICumulativeAmountCommand
{
	private readonly AppDbContext _context;

	public CumulativeAmountCommand(AppDbContext context)
	{
		_context = context;
	}

	public Task Recalculate(Guid accountId, CancellationToken cancellationToken = default) =>
		Execute(accountId, null, 0m, cancellationToken);

	public Task Recalculate(Guid accountId, DateTime fromDateTime, decimal initialAmount, CancellationToken cancellationToken = default) =>
		Execute(accountId, fromDateTime, initialAmount, cancellationToken);

	private async Task Execute(Guid accountId, DateTime? fromDateTime, decimal initialAmount, CancellationToken cancellationToken)
	{
		long seed = ScaledAmount.ToStorage(initialAmount);
		int confirmed = (int)TransactionState.Confirmed;
		await _context.Database.ExecuteSqlInterpolatedAsync($"""
			WITH InputRows AS (
				SELECT NULL AS EntryId, NULL AS DateTime, NULL AS TransactionId,
					NULL AS Position, 1 AS IsSeed, 0 AS IsConfirmed, {seed} AS Contribution
				UNION ALL
				SELECT e.Id, t.DateTime, t.Id, e.Position, 0,
					CASE WHEN t.State = {confirmed} THEN 1 ELSE 0 END,
					CASE WHEN t.State = {confirmed} THEN e.Amount ELSE 0 END
				FROM TransactionEntries AS e
				JOIN Transactions AS t ON t.Id = e.TransactionId
				WHERE e.AccountId = {accountId} AND t.DeleteRevision IS NULL
					AND ({fromDateTime} IS NULL OR t.DateTime >= {fromDateTime})
			),
			Calculated AS (
				SELECT EntryId,
					CASE WHEN IsConfirmed = 1 THEN
						SUM(Contribution) OVER (
							ORDER BY IsSeed DESC, DateTime, TransactionId, Position
							ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
						ELSE 0 END AS NewAmount
				FROM InputRows
			)
			UPDATE TransactionEntries AS target
			SET CumulativeAmount = calculated.NewAmount
			FROM Calculated AS calculated
			WHERE target.Id = calculated.EntryId
				AND target.CumulativeAmount IS NOT calculated.NewAmount
			""", cancellationToken);
	}
}
