using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.Storage;

namespace Shared.Azure.Storage;

/// <summary>
/// Creates Blob processors from the host's blobStorage configuration.
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
		BlobContainerClient container = BlobStorageClientFactory.CreateContainer(_configuration, _credential);
		return new BlobStorageProcessor(container);
	}

}
