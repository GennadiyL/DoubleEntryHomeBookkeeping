namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the outcome of a synchronization attempt.
/// Used to display progress and continue snapshot installation.
/// Status carries the durable attempt key and observable state.
/// Transfer is present when a snapshot download is needed.
/// A no-change outcome may have no transfer descriptor.
/// Absence of a transfer is not by itself proof of success.
/// The returned state determines recovery or acknowledgement work.
/// No business entities or owner secrets are returned.
/// </summary>
public record SynchronizeInfo
{
	public required OperationStatusInfo Status { get; set; }
	public TransferInfo? Transfer { get; set; }
}
