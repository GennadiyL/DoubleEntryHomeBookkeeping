namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Reports the snapshot revision successfully installed by a registered Local copy.
/// MasterDatasetKey and LocalDatasetKey identify the acknowledgement scope.
/// Infrastructure authorizes both identities before invoking the business service.
/// InstalledRevision is the actual downloaded and installed snapshot revision.
/// It may exceed the revision returned by an earlier synchronization call.
/// Master advances SyncRevision and records SyncDateTime using its UTC clock.
/// Repeated receipts must be idempotent and older receipts cannot lower SyncRevision.
/// Downloading without successful installation does not justify this acknowledgement.
/// </summary>
public record AcknowledgeRequest
{
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
	public long InstalledRevision { get; set; }
}
