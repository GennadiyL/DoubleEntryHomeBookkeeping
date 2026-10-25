using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Shared.Azure.Storage;

/// <summary>
/// Acquires a finite lease on an existing blob in the configured storage container.
/// Each acquisition creates a unique proposed lease identity for the supplied object name.
/// Contention returns null without polling or waiting for the current owner.
/// Authentication, storage and cancellation failures propagate to the caller.
/// Failed acquisitions release their proposed identity when possible.
/// Successful acquisition transfers renewal and release to the returned lock.
/// Configuration and authentication follow the same rules as Blob storage processors.
/// The factory has no knowledge of database names or application workflows.
/// </summary>
internal class BlobStorageLockFactory : IStorageLockFactory
{
	private readonly ISharedContext _sharedContext;
	private readonly IConfiguration _configuration;
	private readonly TokenCredential _credential;
	private readonly TimeProvider _clock;

	public BlobStorageLockFactory(ISharedContext sharedContext, IConfiguration configuration,
		TokenCredential credential, TimeProvider clock)
	{
		_sharedContext = sharedContext;
		_configuration = configuration;
		_credential = credential;
		_clock = clock;
	}

	public async Task<IStorageLock?> TryAcquire(string objectName, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
		cancellationToken.ThrowIfCancellationRequested();
		BlobLeaseClient lease = CreateLease(objectName);
		return await Acquire(objectName, lease, cancellationToken);
	}

	protected virtual BlobLeaseClient CreateLease(string objectName)
	{
		BlobContainerClient container = BlobStorageClientFactory.CreateContainer(_configuration, _credential);
		return container.GetBlobClient(objectName).GetBlobLeaseClient(Guid.NewGuid().ToString());
	}

	private async Task<IStorageLock?> Acquire(string objectName, BlobLeaseClient lease, CancellationToken cancellationToken)
	{
		long started = _clock.GetTimestamp();
		try
		{
			await lease.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: cancellationToken);
			return new BlobStorageLock(objectName, lease, _clock, _sharedContext, started);
		}
		catch (RequestFailedException exception) when (IsBusy(exception))
		{
			return null;
		}
		catch
		{
			await BlobStorageLock.Release(lease, _clock, _sharedContext.LogService);
			throw;
		}
	}

	private static bool IsBusy(RequestFailedException exception) =>
		exception.Status == 409 && exception.ErrorCode is "LeaseAlreadyPresent" or "LeaseIsBreakingAndCannotBeAcquired";
}
