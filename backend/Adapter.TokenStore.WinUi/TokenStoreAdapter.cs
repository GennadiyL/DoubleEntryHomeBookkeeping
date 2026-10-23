using System.Security.Cryptography;
using System.Text;
using Shared.Contracts;
using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.TokenStore;

namespace Adapter.TokenStore.WinUi;

/// <summary>
/// Stores Master tokens in separate files protected by current-user Windows DPAPI.
/// The host selects a directory under the current Windows user's local profile.
/// A hash of the complete scope names each file without exposing raw identifiers.
/// The same scope is bound to encryption as DPAPI additional entropy.
/// Reads never return a token from another server, dataset or Local registration.
/// Missing, inaccessible or corrupt stored tokens require authentication again.
/// Replacement writes publish only encrypted bytes through a temporary file.
/// This adapter neither authenticates users nor decides whether tokens are expired.
/// </summary>
internal sealed class TokenStoreAdapter : ITokenStoreAdapter
{
	private readonly ISharedContext _sharedContext;
	private readonly string _directory;

	public TokenStoreAdapter(ISharedContext sharedContext, string directory)
	{
		_sharedContext = sharedContext;
		_directory = directory;
	}

	private static readonly UTF8Encoding TokenEncoding = new(false, true);

	public async Task<string?> Read(TokenStoreScope scope, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		byte[] entropy = GetScopeBytes(scope);
		byte[]? plaintext = null;
		try
		{
			byte[] encrypted = await _sharedContext.FileService.ReadAllBytesAsync(GetPath(entropy), cancellationToken).ConfigureAwait(false);
			plaintext = ProtectedData.Unprotect(encrypted, entropy, DataProtectionScope.CurrentUser);
			cancellationToken.ThrowIfCancellationRequested();
			string token = TokenEncoding.GetString(plaintext);
			return string.IsNullOrWhiteSpace(token) ? null : token;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or DecoderFallbackException)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return null;
		}
		finally
		{
			if (plaintext is not null)
			{
				CryptographicOperations.ZeroMemory(plaintext);
			}
		}
	}

	public async Task Save(TokenStoreScope scope, string token, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(token);
		byte[] entropy = GetScopeBytes(scope);
		byte[] plaintext = TokenEncoding.GetBytes(token);
		byte[] encrypted;
		try
		{
			encrypted = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(plaintext);
		}
		cancellationToken.ThrowIfCancellationRequested();
		_sharedContext.DirectoryService.CreateDirectory(_directory);
		string path = GetPath(entropy);
		string temporaryPath = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			await using (FileStream stream = _sharedContext.FileService.Open(temporaryPath, new FileStreamOptions
			{
				Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
				BufferSize = 4096, Options = FileOptions.Asynchronous | FileOptions.WriteThrough
			}))
			{
				await stream.WriteAsync(encrypted, cancellationToken).ConfigureAwait(false);
				stream.Flush(true);
			}
			cancellationToken.ThrowIfCancellationRequested();
			_sharedContext.FileService.Move(temporaryPath, path, true);
		}
		finally
		{
			_sharedContext.FileService.Delete(temporaryPath);
		}
	}

	public Task Delete(TokenStoreScope scope, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		byte[] entropy = GetScopeBytes(scope);
		try
		{
			_sharedContext.FileService.Delete(GetPath(entropy));
		}
		catch (DirectoryNotFoundException)
		{
		}
		return Task.CompletedTask;
	}

	private string GetPath(byte[] entropy) => Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(entropy)) + ".bin");

	private static byte[] GetScopeBytes(TokenStoreScope scope)
	{
		ArgumentNullException.ThrowIfNull(scope);
		ArgumentNullException.ThrowIfNull(scope.MasterAddress);
		ArgumentException.ThrowIfNullOrWhiteSpace(scope.MasterDatasetKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(scope.LocalDatasetKey);
		Uri address = scope.MasterAddress;
		if (!address.IsAbsoluteUri || !string.IsNullOrEmpty(address.UserInfo) ||
			!string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment) ||
			(address.Scheme != Uri.UriSchemeHttps && !(address.Scheme == Uri.UriSchemeHttp && address.IsLoopback)))
		{
			throw new ArgumentException("The Master address must use HTTPS, or HTTP for localhost, without credentials, query or fragment.", nameof(scope));
		}
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, Encoding.UTF8, true);
		writer.Write(address.AbsoluteUri);
		writer.Write(scope.MasterDatasetKey);
		writer.Write(scope.LocalDatasetKey);
		writer.Flush();
		return stream.ToArray();
	}
}
