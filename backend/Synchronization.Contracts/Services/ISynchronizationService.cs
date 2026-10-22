using Synchronization.Contracts.Services.Synchronizations;

namespace Synchronization.Contracts.Services;

/// <summary>
/// Coordinates synchronization of the current Local dataset.
/// Checks authorization before preparing new changes for remote submission.
/// Retains original batch identities and omits empty batches from the saved document.
/// Owns capture, publication, download, installation and acknowledgement sequencing.
/// Resumes durable pending work, including acknowledgement after installation.
/// Clears saved synchronization data only after acknowledgement succeeds.
/// Cancellation respects atomic Local persistence and installation boundaries.
/// Registered Local copies can submit pending changes regardless of inactivity.
/// Local settings remain outside synchronized business content.
/// </summary>
public interface ISynchronizationService
{
	/// <summary>
	/// Synchronizes or resumes pending work through installation and acknowledgement, returning the outcome for the UI.
	/// Cancellation stops cooperative local work but cannot undo Master publication or discard durable pending work.
	/// </summary>
	public Task<SynchronizeInfo> Synchronize(CancellationToken cancellationToken = default);
}
