using DataAccess.Contracts;

namespace Synchronization.Impl.Operations.AcceptCapture;

/// <summary>
/// Accepts the captured Local state through immediate bulk repository operations.
/// The caller must durably secure the batch and prevent intervening edits.
/// The caller supplies a fresh unit-of-work scope and owns the active transaction.
/// Deletes referencing entities before their referenced catalog entities.
/// Owned transaction and template entries are removed by database cascades.
/// All deletions finish before any surviving entity tracking is accepted.
/// Repository failures propagate so the caller can roll back the entire operation.
/// Live references must already be resolved; this operation does not rewrite business data.
/// No file persistence, transaction commit or Master acknowledgement occurs here.
/// </summary>
internal sealed class AcceptCaptureOperation : IAcceptCaptureOperation
{
	private readonly IAppUnitOfWork _unitOfWork;

	public AcceptCaptureOperation(IAppUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task Run(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await _unitOfWork.SystemConfigRepo.HardDelete(cancellationToken);
		await _unitOfWork.TransactionRepo.HardDelete(cancellationToken);
		await _unitOfWork.TemplateRepo.HardDelete(cancellationToken);
		await _unitOfWork.ReportRepo.HardDelete(cancellationToken);
		await _unitOfWork.AccountRepo.HardDelete(cancellationToken);
		await _unitOfWork.CurrencyRateRepo.HardDelete(cancellationToken);
		await _unitOfWork.CategoryRepo.HardDelete(cancellationToken);
		await _unitOfWork.CorrespondentRepo.HardDelete(cancellationToken);
		await _unitOfWork.ProjectRepo.HardDelete(cancellationToken);
		await _unitOfWork.AccountGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.TemplateGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.ReportGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.CategoryGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.CorrespondentGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.ProjectGroupRepo.HardDelete(cancellationToken);
		await _unitOfWork.CurrencyRepo.HardDelete(cancellationToken);

		await _unitOfWork.SystemConfigRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.TransactionRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.TemplateRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.ReportRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.AccountRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CurrencyRateRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CategoryRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CorrespondentRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.ProjectRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.AccountGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.TemplateGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.ReportGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CategoryGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CorrespondentGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.ProjectGroupRepo.AcceptChanges(cancellationToken);
		await _unitOfWork.CurrencyRepo.AcceptChanges(cancellationToken);
	}
}