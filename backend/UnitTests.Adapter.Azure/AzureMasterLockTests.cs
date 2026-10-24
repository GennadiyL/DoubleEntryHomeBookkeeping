using Adapter.Azure;
using Adapter.Azure.Access;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Master.Contracts.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace UnitTests.Adapter.Azure;

/// <summary>
/// Verifies Azure Master access without connecting to a storage account.
/// Azure SDK lease clients are substituted at the provider boundary.
/// A controlled clock drives renewal and expiration without production delays.
/// Contention is distinguished from authorization and storage failures.
/// Tests cover conservative ownership loss when renewal fails or stalls.
/// Cleanup is checked independently of caller cancellation.
/// Repeated disposal must stop renewal and release the lease only once.
/// These tests do not prove storage permissions or deployed Azure connectivity.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class AzureMasterLockTests
{
	private BlobLeaseClient _lease = null!;
	private FakeTimeProvider _clock = null!;
	private AzureMasterLock _masterLock = null!;

	[SetUp]
	public void SetUp()
	{
		_lease = Substitute.For<BlobLeaseClient>();
		_clock = new FakeTimeProvider();
		_masterLock = new AzureMasterLock(_ => _lease, _clock, NullLogger<AzureMasterLockHandle>.Instance);
	}

	[Test]
	public async Task AcquisitionUsesFiniteLeaseAndReleasesOnce()
	{
		IMasterLockHandle handle = (await _masterLock.TryAcquire("master"))!;
		Assert.That(handle.CancellationToken.IsCancellationRequested, Is.False);
		await _lease.Received(1).AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: Arg.Any<CancellationToken>());
		await Task.WhenAll(handle.DisposeAsync().AsTask(), handle.DisposeAsync().AsTask());
		await _lease.Received(1).ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>());
		_clock.Advance(TimeSpan.FromMinutes(2));
		await _lease.DidNotReceive().RenewAsync(cancellationToken: Arg.Any<CancellationToken>());
	}

	[TestCase("LeaseAlreadyPresent")]
	[TestCase("LeaseIsBreakingAndCannotBeAcquired")]
	public async Task ContentionReturnsNullWithoutRelease(string errorCode)
	{
		FailAcquisition(new RequestFailedException(409, "Busy", errorCode, null));
		Assert.That(await _masterLock.TryAcquire("master"), Is.Null);
		await _lease.DidNotReceive().ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>());
	}

	[TestCase(403, "AuthorizationPermissionMismatch")]
	[TestCase(404, "BlobNotFound")]
	[TestCase(409, "ContainerBeingDeleted")]
	[TestCase(500, "InternalError")]
	public void InfrastructureFailureIsNotReportedAsContention(int status, string errorCode)
	{
		RequestFailedException failure = new(status, "Failure", errorCode, null);
		FailAcquisition(failure);
		Assert.That(Assert.ThrowsAsync<RequestFailedException>(() => _masterLock.TryAcquire("master")), Is.SameAs(failure));
	}

	[Test]
	public async Task PreCancelledRequestDoesNotContactStorage()
	{
		Assert.ThrowsAsync<OperationCanceledException>(() => _masterLock.TryAcquire("master", new CancellationToken(true)));
		await _lease.DidNotReceive().AcquireAsync(Arg.Any<TimeSpan>(), cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task UncertainAcquisitionIsReleasedWithIndependentToken()
	{
		using CancellationTokenSource cancellation = new();
		_lease.AcquireAsync(Arg.Any<TimeSpan>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(_ =>
		{
			cancellation.Cancel();
			return Task.FromException<Response<BlobLease>>(new OperationCanceledException(cancellation.Token));
		});
		Assert.ThrowsAsync<OperationCanceledException>(() => _masterLock.TryAcquire("master", cancellation.Token));
		await _lease.Received(1).ReleaseAsync(cancellationToken: Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
	}

	[Test]
	public async Task SuccessfulRenewalExtendsOwnershipBudget()
	{
		await using IMasterLockHandle? handle = await _masterLock.TryAcquire("master");
		_clock.Advance(TimeSpan.FromSeconds(20));
		await WaitFor(() => RenewalCount == 1);
		_clock.Advance(TimeSpan.FromSeconds(31));
		Assert.That(handle!.CancellationToken.IsCancellationRequested, Is.False);
	}

	[Test]
	public async Task FailedRenewalSignalsOwnershipLoss()
	{
		_lease.RenewAsync(cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new RequestFailedException(412, "Lease lost"));
		await using IMasterLockHandle? handle = await _masterLock.TryAcquire("master");
		_clock.Advance(TimeSpan.FromSeconds(20));
		await WaitFor(() => handle!.CancellationToken.IsCancellationRequested);
		Assert.That(RenewalCount, Is.EqualTo(1));
	}

	[Test]
	public async Task StalledRenewalCannotKeepOwnershipAlive()
	{
		TaskCompletionSource<Response<BlobLease>> stalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_lease.RenewAsync(cancellationToken: Arg.Any<CancellationToken>()).Returns(stalled.Task);
		await using IMasterLockHandle? handle = await _masterLock.TryAcquire("master");
		try
		{
			_clock.Advance(TimeSpan.FromSeconds(20));
			await WaitFor(() => RenewalCount == 1);
			_clock.Advance(TimeSpan.FromSeconds(31));
			Assert.That(handle!.CancellationToken.IsCancellationRequested, Is.True);
		}
		finally
		{
			stalled.TrySetResult(null!);
		}
	}

	[Test]
	public async Task RenewalTimeoutSignalsOwnershipLoss()
	{
		_lease.RenewAsync(cancellationToken: Arg.Any<CancellationToken>())
			.Returns(call => WaitUntilCancelled(call.Arg<CancellationToken>()));
		await using IMasterLockHandle? handle = await _masterLock.TryAcquire("master");
		_clock.Advance(TimeSpan.FromSeconds(20));
		await WaitFor(() => RenewalCount == 1);
		_clock.Advance(TimeSpan.FromSeconds(10));
		await WaitFor(() => handle!.CancellationToken.IsCancellationRequested);
	}

	[Test]
	public async Task DisposalCancelsInFlightRenewalBeforeRelease()
	{
		_lease.RenewAsync(cancellationToken: Arg.Any<CancellationToken>())
			.Returns(call => WaitUntilCancelled(call.Arg<CancellationToken>()));
		IMasterLockHandle handle = (await _masterLock.TryAcquire("master"))!;
		_clock.Advance(TimeSpan.FromSeconds(20));
		await WaitFor(() => RenewalCount == 1);
		await handle.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		await _lease.Received(1).ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task SlowAcquisitionDoesNotAssumeFreshLeaseLifetime()
	{
		_lease.AcquireAsync(Arg.Any<TimeSpan>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(_ =>
		{
			_clock.Advance(TimeSpan.FromSeconds(51));
			return Task.FromResult<Response<BlobLease>>(null!);
		});
		await using IMasterLockHandle? handle = await _masterLock.TryAcquire("master");
		Assert.That(handle!.CancellationToken.IsCancellationRequested, Is.True);
	}

	[Test]
	public async Task CancellationAfterAcquisitionDoesNotCancelRelease()
	{
		using CancellationTokenSource cancellation = new();
		IMasterLockHandle handle = (await _masterLock.TryAcquire("master", cancellation.Token))!;
		await cancellation.CancelAsync();
		await handle.DisposeAsync();
		await _lease.Received(1).ReleaseAsync(cancellationToken: Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
	}

	[Test]
	public async Task ReleaseFailureDoesNotMaskBusinessOutcome()
	{
		_lease.ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new RequestFailedException(503, "Unavailable"));
		IMasterLockHandle handle = (await _masterLock.TryAcquire("master"))!;
		Assert.DoesNotThrowAsync(async () => await handle.DisposeAsync());
	}

	[Test]
	public void RegistrationSharesLockWithoutAccessingStorage()
	{
		ServiceCollection services = new();
		services.AddAdapterAzureModule(_ => throw new InvalidOperationException("Registration must not request a blob."));
		using ServiceProvider provider = services.BuildServiceProvider(true);
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();
		Assert.That(first.ServiceProvider.GetRequiredService<IMasterLock>(), Is.SameAs(second.ServiceProvider.GetRequiredService<IMasterLock>()));
	}

	private int RenewalCount => _lease.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(BlobLeaseClient.RenewAsync));

	private void FailAcquisition(Exception failure) =>
		_lease.AcquireAsync(Arg.Any<TimeSpan>(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(failure);

	private static async Task<Response<BlobLease>> WaitUntilCancelled(CancellationToken cancellationToken)
	{
		await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
		return null!;
	}

	private static async Task WaitFor(Func<bool> condition)
	{
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
		while (!condition())
		{
			await Task.Delay(1, timeout.Token);
		}
	}
}
