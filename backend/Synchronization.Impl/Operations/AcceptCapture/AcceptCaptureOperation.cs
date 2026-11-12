using DataAccess.Contracts;

namespace Synchronization.Impl.Operations.AcceptCapture;

/// <summary>
/// Accepts the captured Local state through immediate bulk repository operations.
/// The caller must durably secure the batch and prevent intervening edits.
/// The caller supplies a fresh unit-of-work scope and owns the active transaction.
/// Hard-deletes captured Local deletions in dependency order.
/// Owned entries are removed through aggregate cascades.
/// Surviving new rows receive zero edit revisions; restore ends at durable capture.
/// Repository failures propagate so the caller can roll back the entire operation.
/// Live references must already be resolved; this operation does not rewrite business data.
/// No file persistence, transaction commit or Master acknowledgement occurs here.
/// </summary>
internal sealed class AcceptCaptureOperation : IAcceptCaptureOperation
{
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;

	public AcceptCaptureOperation(IUnitOfWorkFactory unitOfWorkFactory)
	{
		_unitOfWorkFactory = unitOfWorkFactory;
	}

	public async Task Run(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ILocalUnitOfWork unitOfWork = _unitOfWorkFactory.CreateLocal();

		await unitOfWork.TransactionRepo.HardDelete(cancellationToken);
		await unitOfWork.TemplateRepo.HardDelete(cancellationToken);
		await unitOfWork.ReportRepo.HardDelete(cancellationToken);
		await unitOfWork.AccountRepo.HardDelete(cancellationToken);
		await unitOfWork.CurrencyRateRepo.HardDelete(cancellationToken);
		await unitOfWork.CategoryRepo.HardDelete(cancellationToken);
		await unitOfWork.CorrespondentRepo.HardDelete(cancellationToken);
		await unitOfWork.ProjectRepo.HardDelete(cancellationToken);
		await unitOfWork.AccountGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.TemplateGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.ReportGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.CategoryGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.CorrespondentGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.ProjectGroupRepo.HardDelete(cancellationToken);
		await unitOfWork.CurrencyRepo.HardDelete(cancellationToken);

		await unitOfWork.TransactionRepo.AcceptChanges(cancellationToken);
		await unitOfWork.TemplateRepo.AcceptChanges(cancellationToken);
		await unitOfWork.ReportRepo.AcceptChanges(cancellationToken);
		await unitOfWork.AccountRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CurrencyRateRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CategoryRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CorrespondentRepo.AcceptChanges(cancellationToken);
		await unitOfWork.ProjectRepo.AcceptChanges(cancellationToken);
		await unitOfWork.AccountGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.TemplateGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.ReportGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CategoryGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CorrespondentGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.ProjectGroupRepo.AcceptChanges(cancellationToken);
		await unitOfWork.CurrencyRepo.AcceptChanges(cancellationToken);
	}
}
