using Adapter.Azure;
using Shared.Azure.Storage;
using Adapter.Azure.Storage;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;

namespace UnitTests.Adapter.Azure;

/// <summary>
/// Verifies Azure Master storage with real temporary files and substituted Blob clients.
/// Each test acquires a real Azure handle over a substituted lease client.
/// Downloads and publications must carry that exact lease identity.
/// Wrong handles and ownership loss must prevent storage access.
/// Temporary files are cleaned after successful sessions and partial download failures.
/// Failed publication must prevent reuse after an uncertain server outcome.
/// Storage cleanup must not dispose the caller-owned access handle or snapshot.
/// No Azure account, network connection or application database is used.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class BlobMasterStorageAdapterTests : IAsyncDisposable
{
	private BlobClient _blob = null!;
	private BlobLeaseClient _lease = null!;
	private BlobStorageLock _access = null!;
	private BlobMasterStorageAdapter _storage = null!;
	private DirectoryInfo _snapshots = null!;
	private readonly List<IMasterStorageSession> _sessions = new();
	private string? _downloadPath;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public async Task SetUp()
	{
		_blob = Substitute.For<BlobClient>();
		Uri masterUri = new("https://example.blob.core.windows.net/masters/Master_master.db");
		_blob.Uri.Returns(masterUri);
		_lease = Substitute.For<BlobLeaseClient>();
		_lease.Uri.Returns(masterUri);
		_lease.LeaseId.Returns("lease-id");
		ISharedContext sharedContext = CreateSharedContext();
		await _lease.AcquireAsync(TimeSpan.FromSeconds(60));
		FakeTimeProvider clock = new();
		_access = new BlobStorageLock("Master_master.db", _lease, clock, sharedContext, clock.GetTimestamp());
		_storage = new BlobMasterStorageAdapter(sharedContext, _ => _blob, NullLogger<BlobMasterStorageAdapter>.Instance);
		_snapshots = Directory.CreateTempSubdirectory("dehb-storage-tests-");
		_sessions.Clear();
		ConfigureDownload();
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
		if (_access is not null)
		{
			await _access.DisposeAsync();
		}
		_scope?.Dispose();
		_provider?.Dispose();
		if (_snapshots is not null && Directory.Exists(_snapshots.FullName))
		{
			_snapshots.Delete(true);
		}
		GC.SuppressFinalize(this);
	}

	[Test]
	public async Task OpenDownloadsWithExistingLeaseIntoUniqueDirectories()
	{
		IMasterStorageSession first = await Open();
		IMasterStorageSession second = await Open();
		Assert.That(first.StoragePath, Is.Not.EqualTo(second.StoragePath));
		Assert.That(await File.ReadAllTextAsync(first.StoragePath), Is.EqualTo("database"));
		await _blob.Received(2).DownloadToAsync(Arg.Any<Stream>(),
			Arg.Is<BlobDownloadToOptions>(options => options.Conditions.LeaseId == "lease-id"), Arg.Any<CancellationToken>());
		await _lease.Received(1).AcquireAsync(Arg.Any<TimeSpan>(), cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task PublicationUploadsSnapshotWithExistingLease()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		byte[]? uploaded = null;
		_blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			using MemoryStream copy = new();
			await call.Arg<Stream>().CopyToAsync(copy);
			uploaded = copy.ToArray();
			return (Response<BlobContentInfo>)null!;
		});
		await session.Publish(path);
		Assert.That(uploaded, Is.EqualTo(await File.ReadAllBytesAsync(path)));
		await _blob.Received(1).UploadAsync(Arg.Any<Stream>(),
			Arg.Is<BlobUploadOptions>(options => options.Conditions.LeaseId == "lease-id"), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task DisposalDeletesWorkingFilesButPreservesSnapshotAndLease()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		await File.WriteAllTextAsync(session.StoragePath + "-wal", "sidecar");
		await session.Publish(path);
		await Task.WhenAll(session.DisposeAsync().AsTask(), session.DisposeAsync().AsTask());
		Assert.That(Directory.Exists(Path.GetDirectoryName(session.StoragePath)), Is.False);
		Assert.That(File.Exists(path), Is.True);
		Assert.ThrowsAsync<ObjectDisposedException>(() => session.Publish(path));
		await _lease.DidNotReceive().ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task FailedDownloadRemovesPartialFileAndPreservesOriginalError()
	{
		IOException failure = new("Download interrupted.");
		_blob.DownloadToAsync(Arg.Any<Stream>(), Arg.Any<BlobDownloadToOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			await Download(call.Arg<Stream>(), call.Arg<CancellationToken>());
			return await Task.FromException<Response>(failure);
		});
		Assert.That(Assert.ThrowsAsync<IOException>(() => _storage.Open("master", _access)), Is.SameAs(failure));
		Assert.That(Directory.Exists(Path.GetDirectoryName(_downloadPath)), Is.False);
		await _lease.DidNotReceive().ReleaseAsync(cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task OwnershipLostDuringDownloadRemovesPartialCopy()
	{
		_blob.DownloadToAsync(Arg.Any<Stream>(), Arg.Any<BlobDownloadToOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			Response response = await Download(call.Arg<Stream>(), call.Arg<CancellationToken>());
			await _access.DisposeAsync();
			return response;
		});
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access));
		Assert.That(Directory.Exists(Path.GetDirectoryName(_downloadPath)), Is.False);
		await _blob.DidNotReceiveWithAnyArgs().UploadAsync(default(Stream)!, default(BlobUploadOptions)!, default);
	}

	[Test]
	public async Task WrongProviderHandleIsRejectedBeforeDownload()
	{
		IStorageLock foreign = Substitute.For<IStorageLock>();
		Assert.ThrowsAsync<ArgumentException>(() => _storage.Open("master", foreign));
		await VerifyNoDownload();
	}

	[Test]
	public async Task WrongDatasetHandleIsRejectedBeforeDownload()
	{
		FakeTimeProvider clock = new();
		await using IStorageLock other = new BlobStorageLock("Master_other-master.db", _lease, clock,
			_scope.ServiceProvider.GetRequiredService<ISharedContext>(), clock.GetTimestamp());
		Assert.ThrowsAsync<ArgumentException>(() => _storage.Open("master", other!));
		await VerifyNoDownload();
	}

	[Test]
	public async Task DifferentBlobMappingIsRejectedBeforeDownload()
	{
		_blob.Uri.Returns(new Uri("https://example.blob.core.windows.net/masters/other.db"));
		Assert.ThrowsAsync<ArgumentException>(() => _storage.Open("master", _access));
		await VerifyNoDownload();
	}

	[Test]
	public async Task DisposedAccessIsRejectedBeforeDownload()
	{
		await _access.DisposeAsync();
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access));
		await VerifyNoDownload();
	}

	[Test]
	public async Task CancelledOpenDoesNotDownload()
	{
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access, new CancellationToken(true)));
		await VerifyNoDownload();
	}

	[Test]
	public async Task WorkingDatabaseCannotBePublishedDirectly()
	{
		IMasterStorageSession session = await Open();
		Assert.ThrowsAsync<ArgumentException>(() => session.Publish(session.StoragePath));
		await _blob.DidNotReceiveWithAnyArgs().UploadAsync(default(Stream)!, default(BlobUploadOptions)!, default);
	}

	[Test]
	public async Task FailedPublicationRequiresNewSession()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		RequestFailedException failure = new(503, "Publication outcome unknown.");
		_blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<RequestFailedException>(() => session.Publish(path)), Is.SameAs(failure));
		Assert.ThrowsAsync<InvalidOperationException>(() => session.Publish(path));
		await _blob.Received(1).UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>());
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
		if (loseOwnership) { await _access.DisposeAsync(); }
		else { await caller.CancelAsync(); }
		Assert.CatchAsync<OperationCanceledException>(() => publication);
		Assert.ThrowsAsync<InvalidOperationException>(() => session.Publish(path));
	}

	[Test]
	public async Task ReleasedLeasePreventsPublication()
	{
		IMasterStorageSession session = await Open();
		string path = await Snapshot();
		await _access.DisposeAsync();
		Assert.CatchAsync<OperationCanceledException>(() => session.Publish(path));
		await _blob.DidNotReceiveWithAnyArgs().UploadAsync(default(Stream)!, default(BlobUploadOptions)!, default);
	}

	[Test]
	public void RegistrationResolvesStorageWithoutNetworkAccess()
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddAdapterAzureModule(_ => throw new InvalidOperationException("Unexpected blob access."));
		using ServiceProvider provider = services.BuildServiceProvider(true);
		using IServiceScope scope = provider.CreateScope();
		Assert.That(scope.ServiceProvider.GetRequiredService<IMasterStorageAdapter>(), Is.Not.Null);
	}

	private ISharedContext CreateSharedContext()
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_scope = _provider.CreateScope();
		return _scope.ServiceProvider.GetRequiredService<ISharedContext>();
	}

	private void ConfigureDownload()
	{
		_downloadPath = null;
		_blob.DownloadToAsync(Arg.Any<Stream>(), Arg.Any<BlobDownloadToOptions>(), Arg.Any<CancellationToken>())
			.Returns(call => Download(call.Arg<Stream>(), call.Arg<CancellationToken>()));
	}

	private async Task<Response> Download(Stream destination, CancellationToken cancellationToken)
	{
		_downloadPath = ((FileStream)destination).Name;
		await destination.WriteAsync(System.Text.Encoding.UTF8.GetBytes("database"), cancellationToken);
		return Substitute.For<Response>();
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

	private Task<Response> VerifyNoDownload() =>
		_blob.DidNotReceive().DownloadToAsync(Arg.Any<Stream>(), Arg.Any<BlobDownloadToOptions>(), Arg.Any<CancellationToken>());

	private void WaitDuringUpload(TaskCompletionSource entered)
	{
		_blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			entered.TrySetResult();
			await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
			return (Response<BlobContentInfo>)null!;
		});
	}
}
