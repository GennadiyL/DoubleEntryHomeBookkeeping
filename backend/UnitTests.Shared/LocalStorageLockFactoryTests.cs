using Shared.Contracts.Storage;
using Shared.Impl;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace UnitTests.Shared;

/// <summary>
/// Verifies local object access through the production dependency registration.
/// Each test owns an independent application provider and lock registry.
/// Requests share the singleton across separate service scopes.
/// Competing acquisitions retain their handles until assertions complete.
/// Distinct objects can proceed independently.
/// Cancellation must not silently release another workflow's ownership.
/// Repeated disposal must not release a later owner's acquisition.
/// Tests use no database files or remote services.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class LocalStorageLockFactoryTests
{
	[Test]
	public async Task ScopesShareExclusiveAccess()
	{
		using ServiceProvider provider = CreateProvider();
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();
		IStorageLockFactory storageLockFactory = first.ServiceProvider.GetRequiredService<IStorageLockFactory>();
		IStorageLockFactory other = second.ServiceProvider.GetRequiredService<IStorageLockFactory>();
		await using IStorageLock? handle = await storageLockFactory.TryAcquire("folder/object.bin");
		Assert.That(handle, Is.Not.Null);
		Assert.That(other, Is.SameAs(storageLockFactory));
		Assert.That(await other.TryAcquire("folder/object.bin"), Is.Null);
	}

	[Test]
	public async Task DifferentObjectsCanBeAcquiredTogether()
	{
		using ServiceProvider provider = CreateProvider();
		IStorageLockFactory storageLockFactory = provider.GetRequiredService<IStorageLockFactory>();
		await using IStorageLock? first = await storageLockFactory.TryAcquire("one");
		await using IStorageLock? second = await storageLockFactory.TryAcquire("two");
		Assert.That(first, Is.Not.Null);
		Assert.That(second, Is.Not.Null);
		Assert.That(first!.CancellationToken.CanBeCanceled, Is.False);
	}

	[Test]
	public async Task ConcurrentAcquisitionHasOneOwner()
	{
		using ServiceProvider provider = CreateProvider();
		IStorageLockFactory storageLockFactory = provider.GetRequiredService<IStorageLockFactory>();
		IStorageLock?[] handles = await Task.WhenAll(Enumerable.Range(0, 32)
			.Select(_ => Task.Run(() => storageLockFactory.TryAcquire("folder/object.bin"))));
		try
		{
			Assert.That(handles.Count(handle => handle is not null), Is.EqualTo(1));
		}
		finally
		{
			foreach (IStorageLock handle in handles.OfType<IStorageLock>())
			{
				await handle.DisposeAsync();
			}
		}
	}

	[Test]
	public async Task RepeatedDisposalDoesNotReleaseNextOwner()
	{
		using ServiceProvider provider = CreateProvider();
		IStorageLockFactory storageLockFactory = provider.GetRequiredService<IStorageLockFactory>();
		IStorageLock first = (await storageLockFactory.TryAcquire("folder/object.bin"))!;
		await Task.WhenAll(first.DisposeAsync().AsTask(), first.DisposeAsync().AsTask());
		await using IStorageLock? second = await storageLockFactory.TryAcquire("folder/object.bin");
		await first.DisposeAsync();
		Assert.That(second, Is.Not.Null);
		Assert.That(await storageLockFactory.TryAcquire("folder/object.bin"), Is.Null);
	}

	[Test]
	public async Task CancellationAfterAcquisitionDoesNotReleaseOwnership()
	{
		using ServiceProvider provider = CreateProvider();
		using CancellationTokenSource cancellation = new();
		IStorageLockFactory storageLockFactory = provider.GetRequiredService<IStorageLockFactory>();
		await using IStorageLock? handle = await storageLockFactory.TryAcquire("folder/object.bin", cancellation.Token);
		await cancellation.CancelAsync();
		Assert.That(await storageLockFactory.TryAcquire("folder/object.bin"), Is.Null);
	}

	[Test]
	public async Task CancelledAcquisitionDoesNotTakeOwnership()
	{
		using ServiceProvider provider = CreateProvider();
		IStorageLockFactory storageLockFactory = provider.GetRequiredService<IStorageLockFactory>();
		Assert.ThrowsAsync<OperationCanceledException>(() => storageLockFactory.TryAcquire("folder/object.bin", new CancellationToken(true)));
		await using IStorageLock? handle = await storageLockFactory.TryAcquire("folder/object.bin");
		Assert.That(handle, Is.Not.Null);
	}

	private static ServiceProvider CreateProvider()
	{
		ServiceCollection services = new();
		services.AddLocalStorageModule();
		return services.BuildServiceProvider(true);
	}
}
