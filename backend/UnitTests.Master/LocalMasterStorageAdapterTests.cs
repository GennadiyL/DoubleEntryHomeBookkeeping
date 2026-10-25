using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Master.Impl;
using Shared.Contracts.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Impl;

namespace UnitTests.Master;

/// <summary>
/// Verifies local Master storage through its production dependency registration.
/// Each test creates isolated files under its own temporary directory.
/// Opening a session must use the configured existing file without copying it.
/// Publishing is a no-op because Local SQLite commits already persist their changes.
/// Disposal must preserve both the database and caller-owned snapshot files.
/// Missing files are rejected instead of creating empty databases implicitly.
/// Cancellation and access loss are observed without changing file contents.
/// No user database, real Master lock or Azure resource is accessed.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class LocalMasterStorageAdapterTests
{
	private DirectoryInfo _directory = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IMasterStorageAdapter _storage = null!;
	private IStorageLock _access = null!;
	private string _databasePath = null!;

	[SetUp]
	public async Task SetUp()
	{
		_directory = Directory.CreateTempSubdirectory("dehb-local-storage-tests-");
		_databasePath = Path.Combine(_directory.FullName, "master.db");
		await File.WriteAllTextAsync(_databasePath, "database");
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddLocalMasterStorageAdapter(_ => _databasePath);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		_storage = _scope.ServiceProvider.GetRequiredService<IMasterStorageAdapter>();
		_access = Substitute.For<IStorageLock>();
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
		if (_directory is not null && Directory.Exists(_directory.FullName)) { _directory.Delete(true); }
	}

	[Test]
	public async Task PublicationAndDisposalPreserveDatabaseAndSnapshot()
	{
		string snapshot = Path.Combine(_directory.FullName, "snapshot.db");
		await File.WriteAllTextAsync(snapshot, "snapshot");
		IMasterStorageSession session = await _storage.Open("master", _access);
		Assert.That(session.StoragePath, Is.EqualTo(_databasePath));
		await session.Publish(snapshot);
		await session.DisposeAsync();
		await session.DisposeAsync();
		Assert.That(await File.ReadAllTextAsync(_databasePath), Is.EqualTo("database"));
		Assert.That(await File.ReadAllTextAsync(snapshot), Is.EqualTo("snapshot"));
		await _access.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task NoSnapshotFileIsNeededForLocalPublication()
	{
		await using IMasterStorageSession session = await _storage.Open("master", _access);
		await session.Publish(Path.Combine(_directory.FullName, "not-created.db"));
		Assert.That(Directory.GetFiles(_directory.FullName), Has.Length.EqualTo(1));
	}

	[Test]
	public void MissingDatabaseIsRejectedWithoutCreatingFile()
	{
		File.Delete(_databasePath);
		Assert.ThrowsAsync<FileNotFoundException>(() => _storage.Open("master", _access));
		Assert.That(File.Exists(_databasePath), Is.False);
	}

	[Test]
	public void CancelledOpenIsRejected()
	{
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access, new CancellationToken(true)));
	}

	[Test]
	public void LostAccessPreventsOpen()
	{
		_access.CancellationToken.Returns(new CancellationToken(true));
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Open("master", _access));
	}

	[Test]
	public async Task LostAccessPreventsPublication()
	{
		await using IMasterStorageSession session = await _storage.Open("master", _access);
		_access.CancellationToken.Returns(new CancellationToken(true));
		Assert.CatchAsync<OperationCanceledException>(() => session.Publish(_databasePath));
	}

	[Test]
	public async Task DisposedSessionRejectsPublication()
	{
		IMasterStorageSession session = await _storage.Open("master", _access);
		await session.DisposeAsync();
		Assert.ThrowsAsync<ObjectDisposedException>(() => session.Publish(_databasePath));
		Assert.That(File.Exists(_databasePath), Is.True);
	}

	[Test]
	public async Task CancelledPublicationDoesNotModifyDatabase()
	{
		await using IMasterStorageSession session = await _storage.Open("master", _access);
		Assert.CatchAsync<OperationCanceledException>(() => session.Publish(_databasePath, new CancellationToken(true)));
		Assert.That(await File.ReadAllTextAsync(_databasePath), Is.EqualTo("database"));
	}
}
