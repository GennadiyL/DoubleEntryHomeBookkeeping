using Business.Contracts.Services;
using Business.Contracts.Utils.Merging;
using Business.Impl.Operations.Cumulative;
using Business.Models.Entities;
using DataAccess.Contracts;
using DataAccess.Core.Entities;

namespace Business.Impl.Services;

/// <summary>
/// Owns the outer transaction for complete cumulative rebuilding.
/// Enumerates live accounts and invokes the shared business operation.
/// All provider commands use the same unit-of-work scope.
/// Successful rebuilding commits once at the end of this function.
/// Failure rolls back every account updated by the action.
/// Rollback is attempted even when the caller cancels.
/// The service changes no synchronization metadata.
/// Snapshot installation remains the caller's responsibility.
/// </summary>
internal sealed class CumulativeService : ICumulativeService
{
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly ICumulativeOperation _operation;

	public CumulativeService(IAppUnitOfWork unitOfWork, ICumulativeOperation operation)
	{
		_unitOfWork = unitOfWork;
		_operation = operation;
	}

	public async Task Rebuild(CancellationToken cancellationToken = default)
	{
		IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			ICollection<Account> accounts = await _unitOfWork.AccountRepo.GetAll(cancellationToken);
			foreach (Account account in accounts.Where(account => !account.IsDeleted()))
			{
				await _operation.Recalculate(account.Id, cancellationToken);
			}
			await _unitOfWork.CommitTransaction(transaction, cancellationToken);
		}
		catch
		{
			await _unitOfWork.RollbackTransaction(transaction, CancellationToken.None);
			throw;
		}
	}
}
