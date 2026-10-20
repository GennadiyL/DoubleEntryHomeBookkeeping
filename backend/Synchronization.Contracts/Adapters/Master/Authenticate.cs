namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Reauthorizes an existing registered Local copy against its Master account.
/// Login identifies the existing owner and is not an account-creation request.
/// Password is supplied only for verification and must not be logged.
/// MasterDatasetKey prevents authorization against a different dataset.
/// LocalDatasetKey retains the existing registration during reauthorization.
/// Successful authentication replaces only this registration's prior token.
/// Missing authorization does not create a replacement Local registration.
/// The service owns credential verification in every execution environment.
/// </summary>
public record Authenticate
{
	public required string Login { get; set; }
	public required string Password { get; set; }
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }

	public override string ToString() => nameof(Authenticate);
}
