using Azure.Storage.Blobs.Specialized;
using Shared.Contracts.Storage;
using Shared.Contracts;

namespace Shared.Azure.Storage;

/// <summary>
/// Maintains one acquired Azure lease until the caller disposes the handle.
/// Renewal runs every twenty seconds for a sixty-second lease.
/// A fifty-second ownership budget leaves a safety margin before server expiration.
/// The budget starts before each request and expires even when renewal is stalled.
/// Failed renewal cancels CancellationToken so the protected workflow can stop immediately.
/// Disposal stops and observes renewal before attempting bounded lease release.
/// Cleanup uses an independent token and logs failures without masking business errors.
/// Provider integrations can validate the internal object, blob and lease identities before protected I/O.
/// </summary>
internal sealed class BlobStorageLock : IStorageLock
{
	private readonly BlobLeaseClient _lease;
	private readonly TimeProvider _clock;
	private readonly ISharedContext _sharedContext;
	private readonly CancellationTokenSource _stop = new();
	private readonly CancellationTokenSource _lost;
	private readonly Task _renewal;
	private readonly object _disposeGate = new();
	private Task? _disposal;
	internal BlobStorageLock(
		string objectName,
		BlobLeaseClient lease,
		TimeProvider clock,
		ISharedContext sharedContext,
		long acquisitionStarted)
	{
		ObjectName = objectName;
		_lease = lease;
		_clock = clock;
		_sharedContext = sharedContext;
		_lost = new CancellationTokenSource(Timeout.InfiniteTimeSpan, clock);
		CancellationToken = _lost.Token;
		RefreshOwnership(acquisitionStarted);
		_renewal = RenewUntilStopped();
	}

	public CancellationToken CancellationToken { get; }
	internal string ObjectName { get; }
	internal Uri BlobUri => _lease.Uri;
	internal string LeaseId => _lease.LeaseId;

	public ValueTask DisposeAsync()
	{
		lock (_disposeGate)
		{
			return new ValueTask(_disposal ??= DisposeCore());
		}
	}

	private async Task RenewUntilStopped()
	{
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, CancellationToken);
		try
		{
			await RenewPeriodically(cancellation.Token);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			await _lost.CancelAsync();
			_sharedContext.LogService.Warn("Blob lease renewal failed; ownership is no longer guaranteed.", exception);
		}
	}

	private async Task RenewPeriodically(CancellationToken cancellationToken)
	{
		while (true)
		{
			await Task.Delay(TimeSpan.FromSeconds(20), _clock, cancellationToken);
			await Renew(cancellationToken);
		}
	}

	private async Task Renew(CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10), _clock);
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
		long started = _clock.GetTimestamp();
		await _lease.RenewAsync(cancellationToken: cancellation.Token);
		RefreshOwnership(started);
	}

	private void RefreshOwnership(long requestStarted)
	{
		TimeSpan remaining = TimeSpan.FromSeconds(50) - _clock.GetElapsedTime(requestStarted);
		if (remaining <= TimeSpan.Zero)
		{
			_lost.Cancel();
		}
		else if (!CancellationToken.IsCancellationRequested)
		{
			_lost.CancelAfter(remaining);
		}
	}

	private async Task DisposeCore()
	{
		await _stop.CancelAsync();
		await _lost.CancelAsync();
		try
		{
			await _renewal;
			await Release(_lease, _clock, _sharedContext.LogService);
		}
		finally
		{
			_stop.Dispose();
			_lost.Dispose();
		}
	}

	internal static async Task Release(BlobLeaseClient lease, TimeProvider clock, ILogService logService)
	{
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10), clock);
		try
		{
			await lease.ReleaseAsync(cancellationToken: timeout.Token);
		}
		catch (Exception exception)
		{
			logService.Warn("Blob lease release failed; the finite lease will expire if still held.", exception);
		}
	}
}
