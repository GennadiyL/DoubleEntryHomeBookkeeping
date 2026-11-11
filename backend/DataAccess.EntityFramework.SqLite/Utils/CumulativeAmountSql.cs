using Business.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite.Utils;

/// <summary>
/// Executes the shared SQLite cumulative amount calculation.
/// Receives the database selected by the calling DAL command.
/// Local editing and Candidate preparation use identical arithmetic.
/// The supplied connection preserves the caller's transaction boundary.
/// Account ordering and confirmed-entry filtering remain in SQL.
/// Integer arithmetic detects overflow without floating-point conversion.
/// No unit of work is created or cached by this helper.
/// Connection and transaction lifetimes remain with the caller.
/// </summary>
internal static class CumulativeAmountSql
{
	internal static async Task Execute(
		DatabaseFacade database,
		Guid accountId,
		DateTime? fromDateTime,
		decimal initialAmount,
		CancellationToken cancellationToken)
	{
		long seed = ScaledAmount.ToStorage(initialAmount);
		int confirmed = (int)TransactionState.Confirmed;
		await database.ExecuteSqlInterpolatedAsync($"""
			WITH InputRows AS (
				SELECT NULL AS EntryId, NULL AS DateTime, NULL AS TransactionId,
					NULL AS Position, 1 AS IsSeed, 0 AS IsConfirmed, {seed} AS Contribution
				UNION ALL
				SELECT e.Id, t.DateTime, t.Id, e.Position, 0,
					CASE WHEN t.State = {confirmed} THEN 1 ELSE 0 END,
					CASE WHEN t.State = {confirmed} THEN e.Amount ELSE 0 END
				FROM "TransactionEntry" AS e
				JOIN "Transaction" AS t ON t.Id = e.TransactionId
				WHERE e.AccountId = {accountId.ToByteArray()} AND t.DeleteRevision IS NULL
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
			UPDATE "TransactionEntry" AS target
			SET CumulativeAmount = calculated.NewAmount
			FROM Calculated AS calculated
			WHERE target.Id = calculated.EntryId
				AND target.CumulativeAmount IS NOT calculated.NewAmount
			""", cancellationToken);
	}
}
