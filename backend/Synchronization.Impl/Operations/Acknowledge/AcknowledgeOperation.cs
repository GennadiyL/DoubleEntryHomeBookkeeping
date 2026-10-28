using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Impl.Operations.Acknowledge;

/// <summary>
/// Sends the actual installed revision to the selected Master provider.
/// The workflow invokes this stage only after durable snapshot installation.
/// Provider authentication applies independently to this request.
/// Repeated acknowledgements preserve Master idempotency semantics.
/// A false response preserves the installed database and its saved container.
/// Provider exceptions and caller cancellation propagate to orchestration.
/// This operation never deletes the durable Local container.
/// The synchronization service reports completion and owns subsequent retries.
/// </summary>
internal sealed class AcknowledgeOperation : IAcknowledgeOperation
{
	private readonly IMasterAdapter _master;

	public AcknowledgeOperation(IMasterAdapter master) => _master = master;

	public Task<AcknowledgeResponse> Run(AcknowledgeRequest command, CancellationToken cancellationToken = default) =>
		_master.Acknowledge(command, cancellationToken);
}
