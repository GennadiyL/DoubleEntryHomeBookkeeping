using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.Storage;

namespace Shared.Azure.Blob;

/// <summary>
/// Creates Blob processors from the host's masterBlobStorage configuration.
/// Each creation builds a container client with a snapshot of the configured URI and retry settings.
/// Credentials are injected and reused so token acquisition remains outside JSON configuration.
/// The factory retains dependencies only and never caches processors.
/// Processor creation performs no network access or container provisioning.
/// </summary>
internal sealed class BlobStorageProcessorFactory : IStorageProcessorFactory
{
	private readonly IConfiguration _configuration;
	private readonly TokenCredential _credential;

	public BlobStorageProcessorFactory(
		IConfiguration configuration,
		TokenCredential credential)
	{
		_configuration = configuration;
		_credential = credential;
	}

	public IStorageProcessor CreateProcessor()
	{
		Uri storageUri = GetStorageUri(_configuration);
		BlobClientOptions options = CreateClientOptions(_configuration);
		BlobContainerClient container = new(storageUri, _credential, options);
		return new BlobStorageProcessor(container);
	}

	private static Uri GetStorageUri(IConfiguration configuration)
	{
		string? value = configuration["masterBlobStorage:StorageUri"];
		if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps ||
			string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) ||
			uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
		{
			throw new InvalidOperationException("Configure masterBlobStorage:StorageUri as an HTTPS container URL without credentials, query or fragment.");
		}

		return uri;
	}

	private static BlobClientOptions CreateClientOptions(IConfiguration configuration)
	{
		BlobClientOptions options = new();
		configuration.GetSection("masterBlobStorage:Retry").Bind(options.Retry);
		ValidateRetry(options.Retry);

		return options;
	}

	private static void ValidateRetry(RetryOptions retry)
	{
		if (!Enum.IsDefined(retry.Mode) || retry.MaxRetries < 0 || retry.Delay < TimeSpan.Zero ||
			retry.MaxDelay < TimeSpan.Zero || retry.NetworkTimeout <= TimeSpan.Zero)
		{
			throw new InvalidOperationException("masterBlobStorage:Retry must define a valid mode, nonnegative retries/delays and a positive network timeout.");
		}
	}
}
