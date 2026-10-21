namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Identifies the Master dataset and Local registration targeted by an adapter call.
/// These identifiers are not credentials and do not grant access by themselves.
/// The adapter combines them with its configured Master address to select a token.
/// Master validates ownership and registration on every protected request.
/// The expected dataset must match the authenticated account's actual dataset.
/// A matching login alone does not authorize access to a different dataset.
/// The same identity checks apply when calling the real Master service in-process.
/// Raw tokens remain inside the provider and token-store implementations.
/// </summary>
public record MasterIdentity
{
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
}
