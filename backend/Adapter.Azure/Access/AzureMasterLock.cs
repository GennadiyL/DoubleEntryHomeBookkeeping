using Azure;
using Azure.Storage.Blobs.Specialized;
using Master.Contracts.Access;
using Microsoft.Extensions.Logging;

namespace Adapter.Azure.Access;

/// <summary>
/// Acquires a finite Azure lease on the existing Master database blob.
/// The host-provided factory creates a fresh proposed lease identity per acquisition.
/// Contention returns null without polling or waiting for the current owner.
/// Authentication, storage and cancellation failures propagate to the caller.
/// Failed acquisition attempts release their proposed identity when possible.
/// Successful acquisition transfers renewal and release to an owned handle.
/// Azure lease identifiers remain internal to the infrastructure implementation.
/// Database downloads, publication and business synchronization are outside this class.
/// </summary>
internal sealed class AzureMasterLock(
	Func<string, BlobLeaseClient> createLease, TimeProvider clock, ILogger<AzureMasterLockHandle> logger) : IMasterLock
{
	public async Task<IMasterLockHandle?> TryAcquire(string masterDatasetKey, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		cancellationToken.ThrowIfCancellationRequested();
		BlobLeaseClient lease = createLease(masterDatasetKey);
		return await Acquire(lease, cancellationToken);
	}

	private async Task<IMasterLockHandle?> Acquire(BlobLeaseClient lease, CancellationToken cancellationToken)
	{
		long started = clock.GetTimestamp();
		try
		{
			await lease.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: cancellationToken);
			return new AzureMasterLockHandle(lease, clock, logger, started);
		}
		catch (RequestFailedException exception) when (IsBusy(exception))
		{
			return null;
		}
		catch
		{
			await AzureMasterLockHandle.Release(lease, clock, logger);
			throw;
		}
	}

	private static bool IsBusy(RequestFailedException exception) =>
		exception.Status == 409 && exception.ErrorCode is "LeaseAlreadyPresent" or "LeaseIsBreakingAndCannotBeAcquired";
}
