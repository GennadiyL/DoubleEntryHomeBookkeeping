using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Impl.Operations.Acknowledge;

/// <summary>
/// Defines the internal installation acknowledgement stage of synchronization.
/// Run receives the actual installed snapshot revision and dataset identities.
/// The host establishes owner and Local registration authorization.
/// Receipts must be safe to repeat after a lost response.
/// Only successful local installation permits this operation to run.
/// Acknowledgement failure retains pending recovery state.
/// Completion drives subsequent caller-owned pending-state cleanup.
/// The operation remains internal to the Synchronization workflow.
/// </summary>
internal interface IAcknowledgeOperation
{
	public Task<AcknowledgeResponse> Run(AcknowledgeRequest command, CancellationToken cancellationToken = default);
}
