using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace Shared.Azure.Storage;

/// <summary>
/// Builds container clients for processors and locks from the same storage configuration.
/// Reads the generic blobStorage section and validates the endpoint and retry options.
/// Receives authentication from the host without persisting credentials.
/// Client creation performs no storage I/O.
/// All application-specific object naming remains outside this helper.
/// </summary>
internal static class BlobStorageClientFactory
{
	public static BlobContainerClient CreateContainer(IConfiguration configuration, TokenCredential credential)
	{
		return new BlobContainerClient(GetStorageUri(configuration), credential, CreateClientOptions(configuration));
	}

	private static Uri GetStorageUri(IConfiguration configuration)
	{
		string? value = configuration["blobStorage:StorageUri"];
		if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps ||
			string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) ||
			uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
		{
			throw new InvalidOperationException("Configure blobStorage:StorageUri as an HTTPS container URL without credentials, query or fragment.");
		}

		return uri;
	}

	private static BlobClientOptions CreateClientOptions(IConfiguration configuration)
	{
		BlobClientOptions options = new();
		configuration.GetSection("blobStorage:Retry").Bind(options.Retry);
		ValidateRetry(options.Retry);

		return options;
	}

	private static void ValidateRetry(RetryOptions retry)
	{
		if (!Enum.IsDefined(retry.Mode) || retry.MaxRetries < 0 || retry.Delay < TimeSpan.Zero ||
			retry.MaxDelay < TimeSpan.Zero || retry.NetworkTimeout <= TimeSpan.Zero)
		{
			throw new InvalidOperationException("blobStorage:Retry must define a valid mode, nonnegative retries/delays and a positive network timeout.");
		}
	}
}
