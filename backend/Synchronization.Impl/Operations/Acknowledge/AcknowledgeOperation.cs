using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Impl.Operations.Acknowledge;

/// <summary>
/// Reserves the internal acknowledge stage of the synchronization workflow.
/// Registered with scoped lifetime by the Synchronization module.
/// Concrete provider and persistence dependencies are introduced with implementation.
/// Run currently throws rather than reporting an unperformed successful action.
/// It performs no remote request or database modification in this scaffold.
/// Durable recovery remains the responsibility of the complete workflow.
/// Cancellation will be passed through the eventual stage implementation.
/// The class is internal and does not expose an additional UI-facing API.
/// </summary>
internal sealed class AcknowledgeOperation : IAcknowledgeOperation
{
	public Task<AcknowledgeResponse> Run(AcknowledgeRequest command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
