namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Returns nonsecret metadata after authentication and secure token persistence.
/// Identity identifies the dataset and Local registration actually authorized.
/// The adapter validates these identifiers against the authentication command.
/// ExpiresAt is the token expiration instant using the Master UTC clock.
/// Token expiry requires reauthentication without expiring the Local copy.
/// The issued token is stored through ITokenStoreAdapter before success is returned.
/// Neither passwords nor raw tokens are exposed in this result.
/// Subsequent provider calls retrieve their own scoped authorization.
/// </summary>
public record AuthenticateInfo
{
	public required MasterIdentity Identity { get; set; }
	public DateTime ExpiresAt { get; set; }
}
