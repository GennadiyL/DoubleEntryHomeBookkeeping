namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Acknowledges a snapshot successfully installed by a local copy.
/// Used after activation of the downloaded business database.
/// InstalledVersion is the actual installed revision.
/// SyncKey identifies the durable attempt being acknowledged.
/// LocalDatasetKey identifies the owned registered copy.
/// Repeated receipts must be safe after a lost response.
/// A download alone does not justify sending this receipt.
/// The operation advances acknowledgement state, not business content.
/// </summary>
public record AcknowledgeSnapshot
{
	public required string SyncKey { get; set; }
	public required string LocalDatasetKey { get; set; }
	public long InstalledVersion { get; set; }
}
