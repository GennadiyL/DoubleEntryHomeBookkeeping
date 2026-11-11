using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.Contracts;
using DataAccess.EntityFramework.SqLite.Utils;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;

	public CumulativeAmountCommand(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

	public Task Recalculate(Guid accountId, CancellationToken cancellationToken = default) =>
		Execute(accountId, null, 0m, cancellationToken);

	public Task Recalculate(
		Guid accountId,
		DateTime fromDateTime,
		decimal initialAmount,
		CancellationToken cancellationToken = default) =>
		Execute(accountId, fromDateTime, initialAmount, cancellationToken);

	private async Task Execute(Guid accountId, DateTime? fromDateTime, decimal initialAmount, CancellationToken cancellationToken)
	{
		IEntityFrameworkUnitOfWork unitOfWork = (IEntityFrameworkUnitOfWork)_unitOfWorkFactory.CreateLocal();
		DatabaseFacade database = unitOfWork.Database;

		await CumulativeAmountSql.Execute(
			database,
			accountId,
			fromDateTime,
			initialAmount,
			cancellationToken);
	}
}
