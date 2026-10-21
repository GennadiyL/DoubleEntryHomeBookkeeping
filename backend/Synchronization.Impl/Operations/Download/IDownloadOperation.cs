using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Impl.Operations.Download;

/// <summary>
/// Defines the internal download and installation stage of synchronization.
/// Run receives the selected immutable snapshot and nonsecret Master identity.
/// The implementation must download into a candidate database before activation.
/// Schema checks, Local settings preservation and cumulative rebuild precede activation.
/// Returns the actual installed version only after successful activation.
/// Partial downloads and installation failures must preserve durable recovery state.
/// It must not acknowledge a download before the database becomes usable.
/// The operation remains internal to the Synchronization workflow.
/// </summary>
internal interface IDownloadOperation
{
	public Task<long> Run(MasterIdentity identity, DownloadSnapshot command, CancellationToken cancellationToken = default);
}
