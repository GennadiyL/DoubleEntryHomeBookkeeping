using Azure.Core;

namespace UnitTests.Shared;

/// <summary>
/// Supplies a fixed test token to real Azure SDK authentication policies.
/// Allows requests to verify that each descriptor selects its own credential.
/// Performs no authentication or network access.
/// Tokens remain valid for the lifetime of a test.
/// </summary>
internal sealed class BlobStorageTestCredential : TokenCredential
{
	private readonly AccessToken _token;

	public BlobStorageTestCredential(string token)
	{
		_token = new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
	}

	public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _token;
	}

	public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return ValueTask.FromResult(_token);
	}
}
