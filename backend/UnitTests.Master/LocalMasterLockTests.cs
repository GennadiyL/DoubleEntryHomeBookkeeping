using Master.Contracts.Access;
using Master.Impl;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace UnitTests.Master;

/// <summary>
/// Verifies local Master access through the production dependency registration.
/// Each test owns an independent application provider and lock registry.
/// Requests share the singleton across separate service scopes.
/// Competing acquisitions retain their handles until assertions complete.
/// Distinct datasets can proceed independently.
/// Cancellation must not silently release another workflow's ownership.
/// Repeated disposal must not release a later owner's acquisition.
/// Tests use no database files or remote services.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class LocalMasterLockTests
{
	[Test]
	public async Task ScopesShareExclusiveAccess()
	{
		using ServiceProvider provider = CreateProvider();
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();
		IMasterLock masterLock = first.ServiceProvider.GetRequiredService<IMasterLock>();
		IMasterLock other = second.ServiceProvider.GetRequiredService<IMasterLock>();
		await using IMasterLockHandle? handle = await masterLock.TryAcquire("master");
		Assert.That(handle, Is.Not.Null);
		Assert.That(other, Is.SameAs(masterLock));
		Assert.That(await other.TryAcquire("master"), Is.Null);
	}

	[Test]
	public async Task DifferentDatasetsCanBeAcquiredTogether()
	{
		using ServiceProvider provider = CreateProvider();
		IMasterLock masterLock = provider.GetRequiredService<IMasterLock>();
		await using IMasterLockHandle? first = await masterLock.TryAcquire("one");
		await using IMasterLockHandle? second = await masterLock.TryAcquire("two");
		Assert.That(first, Is.Not.Null);
		Assert.That(second, Is.Not.Null);
		Assert.That(first!.LockLost.CanBeCanceled, Is.False);
	}

	[Test]
	public async Task ConcurrentAcquisitionHasOneOwner()
	{
		using ServiceProvider provider = CreateProvider();
		IMasterLock masterLock = provider.GetRequiredService<IMasterLock>();
		IMasterLockHandle?[] handles = await Task.WhenAll(Enumerable.Range(0, 32)
			.Select(_ => Task.Run(() => masterLock.TryAcquire("master"))));
		try
		{
			Assert.That(handles.Count(handle => handle is not null), Is.EqualTo(1));
		}
		finally
		{
			foreach (IMasterLockHandle handle in handles.OfType<IMasterLockHandle>())
			{
				await handle.DisposeAsync();
			}
		}
	}

	[Test]
	public async Task RepeatedDisposalDoesNotReleaseNextOwner()
	{
		using ServiceProvider provider = CreateProvider();
		IMasterLock masterLock = provider.GetRequiredService<IMasterLock>();
		IMasterLockHandle first = (await masterLock.TryAcquire("master"))!;
		await Task.WhenAll(first.DisposeAsync().AsTask(), first.DisposeAsync().AsTask());
		await using IMasterLockHandle? second = await masterLock.TryAcquire("master");
		await first.DisposeAsync();
		Assert.That(second, Is.Not.Null);
		Assert.That(await masterLock.TryAcquire("master"), Is.Null);
	}

	[Test]
	public async Task CancellationAfterAcquisitionDoesNotReleaseOwnership()
	{
		using ServiceProvider provider = CreateProvider();
		using CancellationTokenSource cancellation = new();
		IMasterLock masterLock = provider.GetRequiredService<IMasterLock>();
		await using IMasterLockHandle? handle = await masterLock.TryAcquire("master", cancellation.Token);
		await cancellation.CancelAsync();
		Assert.That(await masterLock.TryAcquire("master"), Is.Null);
	}

	[Test]
	public async Task CancelledAcquisitionDoesNotTakeOwnership()
	{
		using ServiceProvider provider = CreateProvider();
		IMasterLock masterLock = provider.GetRequiredService<IMasterLock>();
		Assert.ThrowsAsync<OperationCanceledException>(() => masterLock.TryAcquire("master", new CancellationToken(true)));
		await using IMasterLockHandle? handle = await masterLock.TryAcquire("master");
		Assert.That(handle, Is.Not.Null);
	}

	private static ServiceProvider CreateProvider()
	{
		ServiceCollection services = new();
		services.AddLocalMasterAccess();
		return services.BuildServiceProvider(true);
	}
}
