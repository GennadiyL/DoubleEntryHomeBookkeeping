namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Returns newly issued synchronization authorization for a Local registration.
/// Authorization identifies the dataset and registration actually authorized.
/// The client verifies these identities against its existing local context.
/// ExpiresAt is the token expiration instant using the Master UTC clock.
/// Token expiry is distinct from the registered Local copy's expiry.
/// The client protects the token in its local credential storage.
/// Passwords and persisted password hashes are never returned.
/// This sensitive result must not be included in diagnostics.
/// </summary>
public record AuthenticateInfo
{
	public required MasterAuthorization Authorization { get; set; }
	public DateTime ExpiresAt { get; set; }
	
	public override string ToString() => nameof(AuthenticateInfo);
}
