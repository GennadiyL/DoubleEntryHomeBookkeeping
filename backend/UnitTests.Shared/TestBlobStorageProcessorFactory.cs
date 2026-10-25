using Azure.Storage.Blobs;
using Shared.Azure.Storage;
using Shared.Contracts.Storage;

namespace UnitTests.Shared;

/// <summary>
/// Creates real Blob processors with a client configured by the test fixture.
/// Implements the shared factory contract without changing production construction.
/// The supplied client uses an in-memory HTTP transport and test credentials.
/// Each creation returns a fresh processor bound to that client.
/// The fixture retains ownership of its HTTP client and handler.
/// </summary>
internal sealed class TestBlobStorageProcessorFactory : IStorageProcessorFactory
{
	private readonly BlobContainerClient _container;

	public TestBlobStorageProcessorFactory(BlobContainerClient container)
	{
		_container = container;
	}

	public IStorageProcessor CreateProcessor()
	{
		return new BlobStorageProcessor(_container);
	}
}
