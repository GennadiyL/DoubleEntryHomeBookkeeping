using Azure.Core;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shared.Azure.Storage;
using Shared.Contracts;

namespace UnitTests.Shared;

/// <summary>
/// Substitutes lease operations while exercising the production acquisition workflow.
/// The production constructor receives configuration and authentication dependencies normally.
/// Only lease-client creation is overridden for deterministic local tests.
/// Real client construction can also be inspected without sending network requests.
/// The caller owns the supplied clock, context and lease substitute.
/// </summary>
internal sealed class TestBlobStorageLockFactory : BlobStorageLockFactory
{
	private readonly BlobLeaseClient _lease;

	public TestBlobStorageLockFactory(BlobLeaseClient lease, TimeProvider clock, ISharedContext sharedContext)
		: base(sharedContext, CreateConfiguration(), Substitute.For<TokenCredential>(), clock)
	{
		_lease = lease;
	}

	protected override BlobLeaseClient CreateLease(string objectName)
	{
		return _lease;
	}

	public BlobLeaseClient CreateConfiguredLease(string objectName)
	{
		return base.CreateLease(objectName);
	}

	private static IConfiguration CreateConfiguration()
	{
		return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["blobStorage:StorageUri"] = "https://example.blob.core.windows.net/objects"
		}).Build();
	}
}
