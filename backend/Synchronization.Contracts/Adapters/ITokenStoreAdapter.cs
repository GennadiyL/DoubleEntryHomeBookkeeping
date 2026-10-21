using Synchronization.Contracts.Adapters.TokenStore;

namespace Synchronization.Contracts.Adapters;

/// <summary>
/// Provides protected persistence for the Master provider's synchronization tokens.
/// Tokens are isolated by server address, Master dataset and Local registration.
/// Provider implementations read a token for each protected request.
/// Authentication saves the newly issued token after validating its scope.
/// Missing or unreadable stored data is treated as absent authorization.
/// Storage does not validate credentials, determine expiry or grant access.
/// Windows implementations protect persisted bytes for the current Windows user.
/// Tokens must never be written to logs, business databases or sync documents.
/// </summary>
public interface ITokenStoreAdapter
{
	/// <summary>
	/// Reads the token for the exact scope, returning null when it is missing or unreadable so the provider can request authentication.
	/// </summary>
	public Task<string?> Read(TokenStoreScope scope, CancellationToken cancellationToken = default);

	/// <summary>
	/// Securely replaces the token for the exact scope after successful authentication; persistence failures propagate to the caller.
	/// </summary>
	public Task Save(TokenStoreScope scope, string token, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes only the token for the exact scope; deleting an absent token succeeds and network failures do not justify deletion.
	/// </summary>
	public Task Delete(TokenStoreScope scope, CancellationToken cancellationToken = default);
}
