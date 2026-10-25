using Adapter.Azure;
using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Azure;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Adapter.Azure;

/// <summary>
/// Verifies Master storage through shared processor and lock contracts.
/// Each fixture uses production adapter registration with a substituted processor factory.
/// Real temporary files exercise download, publication and directory cleanup.
/// The adapter forwards object names and caller-owned locks without provider casts.
/// Provider-specific lease validation is covered by Shared storage tests.
/// Failed publication prevents reuse after an uncertain storage outcome.
/// Cancellation and ownership loss interrupt active publication.
/// No Azure account or network connection is used.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class BlobMasterStorageAdapterTests : IAsyncDisposable
{
	private IStorageProcessor _processor = null!;
	private IStorageProcessorFactory _factory = null!;
	private IStorageLock _access = null!;
	private CancellationTokenSource _ownership = null!;
	private IMasterStorageAdapter _storage = null!;
	private DirectoryInfo _snapshots = null!;
	private readonly List<IMasterStorageSession> _sessions = new();
	private string? _downloadPath;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public void SetUp()
	{
		_processor = Substitute.For<IStorageProcessor>();
		_factory = Substitute.For<IStorageProcessorFactory>();
		_factory.CreateProcessor().Returns(_processor);
		_ownership = new CancellationTokenSource();
		_access = Substitute.For<IStorageLock>();
		_access.CancellationToken.Returns(_ownership.Token);
		CreateStorage();
		_snapshots = Directory.CreateTempSubdirectory("dehb-storage-tests-");
		_sessions.Clear();
		_downloadPath = null;
		_processor.Read(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IStorageLock>(), Arg.Any<CancellationToken>())
			.Returns(call => Download(call.Arg<Stream>(), call.Arg<CancellationToken>()));
	}

	private void CreateStorage()
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddAdapterAzureModule();
		services.AddScoped<IStorageProcessorFactory>(_ => _factory);
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_scope = _provider.CreateScope();
		_storage = _scope.ServiceProvider.GetRequiredService<IMasterStorageAdapter>();
	}

	[TearDown]
	public async Task TearDown()
	{
		await DisposeAsync();
	}

	public async ValueTask DisposeAsync()
	{
		foreach (IMasterStorageSession session in _sessions)
		{
			await session.DisposeAsync();
		}
		_ownership?.Dispose();
		_scope?.Dispose();
		_provider?.Dispose();
		if (_snapshots is not null && Directory.Exists(_snapshots.FullName))
		{
			_snapshots.Delete(true);
		}
		GC.SuppressFinalize(this);
	}

	[Test]
	public async Task OpenUsesFactoryAndForwardsObjectAndLockIntoUniqueDirectories()
	{
		IMasterStorageSession first = await Open();
		IMasterStorageSession second = await Open();
		Assert.That(first.StoragePath, Is.Not.EqualTo(second.StoragePath));
		Assert.That(await File.ReadAllTextAsync(first.StoragePath), Is.EqualTo("database"));
		_factory.Received(2).CreateProcessor();
		await _processor.Received(2).Read("Master_master.db", Arg.Any<Stream>(), _access, Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task PublicationUsesSameProcessorObjectAndLock()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		byte[]? uploaded = null;
		_processor.Write(Arg.Any<string>(), Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>()).Returns(async call =>
		{
			using MemoryStream copy = new();
			await call.Arg<Stream>().CopyToAsync(copy);
			uploaded = copy.ToArray();
		});
		await session.Publish(path);
		Assert.That(uploaded, Is.EqualTo(await File.ReadAllBytesAsync(path)));
		_factory.Received(1).CreateProcessor();
		await _processor.Received(1).Write("Master_master.db", Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task DisposalDeletesWorkingFilesButPreservesSnapshotAndLock()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		await File.WriteAllTextAsync(session.StoragePath + "-wal", "sidecar");
		await session.Publish(path);
		await Task.WhenAll(session.DisposeAsync().AsTask(), session.DisposeAsync().AsTask());
		Assert.That(Directory.Exists(Path.GetDirectoryName(session.StoragePath)), Is.False);
		Assert.That(File.Exists(path), Is.True);
		Assert.ThrowsAsync<ObjectDisposedException>(() => session.Publish(path));
		await _access.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task FailedDownloadRemovesPartialFileAndPreservesOriginalError()
	{
		IOException failure = new("Download interrupted.");
		_processor.Read(Arg.Any<string>(), Arg.Any<Stream>(), _access, Arg.Any<CancellationToken>()).Returns(async call =>
		{
			await Download(call.Arg<Stream>(), call.Arg<CancellationToken>());
			throw failure;
		});
		Assert.That(Assert.ThrowsAsync<IOException>(() => _storage.Open("master", _access)), Is.SameAs(failure));
		Assert.That(Directory.Exists(Path.GetDirectoryName(_downloadPath)), Is.False);
		await _access.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task OwnershipLostDuringDownloadRemovesPartialCopy()
	{
		_processor.Read(Arg.Any<string>(), Arg.Any<Stream>(), _access, Arg.Any<CancellationToken>()).Returns(async call =>
		{
			await Download(call.Arg<Stream>(), call.Arg<CancellationToken>());
			await _ownership.CancelAsync();
		});
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access));
		Assert.That(Directory.Exists(Path.GetDirectoryName(_downloadPath)), Is.False);
		await VerifyNoPublication();
	}

	[Test]
	public async Task ProviderValidationFailureCleansTemporaryDirectory()
	{
		ArgumentException failure = new("The lock does not own this storage object.");
		_processor.Read(Arg.Any<string>(), Arg.Any<Stream>(), _access, Arg.Any<CancellationToken>()).Returns(call =>
		{
			_downloadPath = ((FileStream)call.Arg<Stream>()).Name;
			throw failure;
		});
		Assert.That(Assert.ThrowsAsync<ArgumentException>(() => _storage.Open("master", _access)), Is.SameAs(failure));
		Assert.That(Directory.Exists(Path.GetDirectoryName(_downloadPath)), Is.False);
		await _access.DidNotReceive().DisposeAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CancelledOpenDoesNotCreateProcessor(bool loseOwnership)
	{
		if (loseOwnership)
		{
			await _ownership.CancelAsync();
		}

		CancellationToken caller = new(!loseOwnership);
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access, caller));
		_factory.DidNotReceive().CreateProcessor();
	}

	[Test]
	public async Task WorkingDatabaseCannotBePublishedDirectly()
	{
		IMasterStorageSession session = await Open();
		Assert.ThrowsAsync<ArgumentException>(() => session.Publish(session.StoragePath));
		await VerifyNoPublication();
	}

	[Test]
	public async Task FailedPublicationRequiresNewSession()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		IOException failure = new("Publication outcome unknown.");
		_processor.Write(Arg.Any<string>(), Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<IOException>(() => session.Publish(path)), Is.SameAs(failure));
		Assert.ThrowsAsync<InvalidOperationException>(() => session.Publish(path));
		await _processor.Received(1).Write("Master_master.db", Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublicationStopsOnCancellationOrOwnershipLoss(bool loseOwnership)
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		using CancellationTokenSource caller = new();
		TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		WaitDuringUpload(entered);
		Task publication = session.Publish(path, caller.Token);
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await (loseOwnership ? _ownership : caller).CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(() => publication.WaitAsync(TimeSpan.FromSeconds(5)));
		if (loseOwnership)
		{
			Assert.CatchAsync<OperationCanceledException>(() => session.Publish(path));
		}
		else
		{
			Assert.ThrowsAsync<InvalidOperationException>(() => session.Publish(path));
		}
		await _processor.Received(1).Write("Master_master.db", Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task OwnershipLossPreventsPublication()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		await _ownership.CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(() => session.Publish(path));
		await VerifyNoPublication();
	}

	[Test]
	public void HostRegistersAzureProviderAndAdapterSeparatelyWithoutNetworkAccess()
	{
		ServiceCollection services = new();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddSharedModule();
		services.AddSharedAzureModule();
		services.AddAdapterAzureModule();
		using ServiceProvider provider = services.BuildServiceProvider(true);
		using IServiceScope scope = provider.CreateScope();
		Assert.That(scope.ServiceProvider.GetRequiredService<IMasterStorageAdapter>(), Is.Not.Null);
		Assert.That(scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().GetType().Assembly.GetName().Name,
			Is.EqualTo("Shared.Azure"));
		Assert.That(scope.ServiceProvider.GetRequiredService<IStorageLockFactory>().GetType().Assembly.GetName().Name,
			Is.EqualTo("Shared.Azure"));
	}

	private async Task Download(Stream destination, CancellationToken cancellationToken)
	{
		_downloadPath = ((FileStream)destination).Name;
		await destination.WriteAsync(System.Text.Encoding.UTF8.GetBytes("database"), cancellationToken);
	}

	private async Task<IMasterStorageSession> Open()
	{
		IMasterStorageSession session = await _storage.Open("master", _access);
		_sessions.Add(session);
		return session;
	}

	private async Task<string> Snapshot()
	{
		string path = Path.Combine(_snapshots.FullName, $"{Guid.NewGuid():N}.db");
		await File.WriteAllTextAsync(path, "completed-snapshot");
		return path;
	}

	private Task VerifyNoPublication() =>
		_processor.DidNotReceiveWithAnyArgs().Write(default!, default!, default, default, default);

	private void WaitDuringUpload(TaskCompletionSource entered)
	{
		_processor.Write(Arg.Any<string>(), Arg.Any<Stream>(), true, _access, Arg.Any<CancellationToken>()).Returns(async call =>
		{
			entered.TrySetResult();
			await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
		});
	}
}
