namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Requests a consistent current snapshot for the identified Local registration.
/// MasterDatasetKey selects the intended Master dataset.
/// Infrastructure verifies both identities against the authenticated caller.
/// MinimumRevision is a validation lower bound, not an exact snapshot selector.
/// It can use the synchronization result or the revision already installed locally.
/// Zero permits any initialized revision when no higher minimum is required.
/// No preceding synchronization call or transfer token is required.
/// Client checks for unsynchronized database and pending JSON changes before replacement.
/// </summary>
public record DownloadRequest
{
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
	public long MinimumRevision { get; set; }
}
