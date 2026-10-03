using DataAccess.Contracts;
using DataAccess.Contracts.Commands;

namespace Business.Impl.Operations.Cumulative;

/// <summary>
/// Supplies the business opening-balance fallback for cache maintenance.
/// Reads a preceding value only for the ranged variant.
/// A missing predecessor means the account starts from zero.
/// A found zero value remains a valid predecessor result.
/// Delegates persistence to the provider-specific command.
/// Does not load entry graphs or perform per-entry writes.
/// Does not create, commit, inspect or roll back transactions.
/// Service orchestration supplies the shared persistence scope.
/// </summary>
internal sealed class CumulativeOperation : ICumulativeOperation
{
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly ICumulativeAmountCommand _command;

	public CumulativeOperation(IAppUnitOfWork unitOfWork, ICumulativeAmountCommand command)
	{
		_unitOfWork = unitOfWork;
		_command = command;
	}

	public Task RecalculateAsync(Guid accountId, CancellationToken cancellationToken = default) =>
		_command.RecalculateAsync(accountId, cancellationToken);

	public async Task RecalculateAsync(Guid accountId, DateTime fromDateTime, CancellationToken cancellationToken = default)
	{
		decimal? previous = await _unitOfWork.TransactionEntryRepo.GetPreviousAsync(accountId, fromDateTime, cancellationToken);
		await _command.RecalculateAsync(accountId, fromDateTime, previous ?? 0m, cancellationToken);
	}
}
