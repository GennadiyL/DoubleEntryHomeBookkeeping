namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Supplies synchronization authorization for a registered Local copy.
/// The token is issued by Master after successful authentication.
/// MasterDatasetKey identifies the expected dataset, not merely a login.
/// LocalDatasetKey identifies the registration authorized by the token.
/// The service validates token ownership, expiry and both keys on every call.
/// The same checks apply to HTTP and in-process callers.
/// This record grants no access merely by containing matching identifiers.
/// The token must never appear in logs or diagnostic output.
/// </summary>
public record MasterAuthorization
{
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
	public required string Token { get; set; }

	public override string ToString() => nameof(MasterAuthorization);
}
