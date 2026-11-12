using Business.Models.Constants;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl.Operations.MasterStorage;
using Exchange.Models.Synchronizations;
using Exchange.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NSubstitute;
using Shared.Contracts.Storage;

namespace UnitTests.Master;

/// <summary>
/// Verifies publication and cleanup through the common storage operation.
/// Persistent and temporary roots are separate real filesystem directories.
/// Fault injection wraps the production processor at the upload boundary.
/// A failed Master upload must retain its previous revision and receipts.
/// Orphan snapshots can be replaced by the next accepted batch of that revision.
/// Acknowledgements update only the main database and retain published snapshots.
/// Temporary cleanup is confined to operation directories below the configured root.
/// Azure transport and lease behavior remain covered by Shared storage tests.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the temporary store.")]
[TestFixture(Category = "Local")]
public sealed class MasterStorageOperationTests
{
	private MasterTestStore _store = null!;

	[SetUp]
	public async Task SetUp()
	{
		_store = new MasterTestStore();
		await _store.Initialize();
	}

	[TearDown]
	public void TearDown() => _store.Dispose();

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedUploadKeepsPublishedMasterAndRetryReplacesOrphan(bool failSnapshot)
	{
		using ServiceProvider baseline = _store.CreateProvider();
		using IServiceScope baselineScope = baseline.CreateScope();
		IStorageProcessor real = baselineScope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
		IStorageProcessor storage = Substitute.For<IStorageProcessor>();
		IStorageProcessorFactory factory = Substitute.For<IStorageProcessorFactory>();
		factory.CreateProcessor().Returns(storage);
		storage.Read(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IStorageLock?>(), Arg.Any<CancellationToken>()).Returns(call =>
			real.Read(call.ArgAt<string>(0), call.ArgAt<Stream>(1), call.ArgAt<IStorageLock?>(2), call.ArgAt<CancellationToken>(3)));
		List<string> writes = [];
		storage.Write(Arg.Any<string>(), Arg.Any<Stream>(), true, Arg.Any<IStorageLock?>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			string name = call.ArgAt<string>(0);
			writes.Add(name);
			bool snapshot = name.EndsWith(StorageValues.SnapshotFileName(1), StringComparison.Ordinal);
			FileStream source = (FileStream)call.ArgAt<Stream>(1);
			Assert.That(source.Name, Is.EqualTo(Path.Combine(_store.TempRoot, StorageValues.MasterDirectoryName("master"), StorageValues.SnapshotFileName(1))));
			Assert.That(Directory.Exists(Path.Combine(_store.TempRoot, StorageValues.MasterDirectoryName("master"), "Upload")), Is.False);
			Assert.That(call.ArgAt<IStorageLock?>(3) is null, Is.EqualTo(snapshot));
			if (snapshot == failSnapshot)
			{
				throw new IOException("Upload failed.");
			}
			await real.Write(name, call.ArgAt<Stream>(1), true, call.ArgAt<IStorageLock?>(3), call.ArgAt<CancellationToken>(4));
		});
		using (ServiceProvider provider = _store.CreateProvider(services => services.AddSingleton(factory)))
		using (IServiceScope scope = provider.CreateScope())
		{
			Assert.ThrowsAsync<IOException>(() => scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(Request("failed")));
		}
		Assert.That(await _store.Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.Zero);
		Assert.That(await _store.Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.Zero);
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
		Assert.That(writes[0], Is.EqualTo("Master_master/snapshot_1.db"));
		Assert.That(writes.Count, Is.EqualTo(failSnapshot ? 1 : 2));
		using (IServiceScope scope = baseline.CreateScope())
		{
			Assert.That((await scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(Request("replacement"))).Revision, Is.EqualTo(1));
		}
		Assert.That(await _store.Scalar("SELECT COUNT(*) FROM LocalSyncBatch WHERE SyncKey = 'replacement'"), Is.EqualTo(1));
		await using Microsoft.Data.Sqlite.SqliteConnection snapshotConnection = new($"Data Source={Path.Combine(_store.DatasetDirectory, StorageValues.SnapshotFileName(1))};Pooling=False");
		await snapshotConnection.OpenAsync();
		await using var command = snapshotConnection.CreateCommand();
		command.CommandText = "SELECT COUNT(*) FROM LocalSyncBatch WHERE SyncKey = 'replacement'";
		Assert.That((long)(await command.ExecuteScalarAsync())!, Is.EqualTo(1));
	}

	[Test]
	public async Task AcknowledgePreservesSnapshotBytesAndCleansWorkingFiles()
	{
		using ServiceProvider provider = _store.CreateProvider();
		using (IServiceScope scope = provider.CreateScope())
		{
			await scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(Request("first"));
		}
		string path = Path.Combine(_store.DatasetDirectory, StorageValues.SnapshotFileName(1));
		byte[] original = await File.ReadAllBytesAsync(path);
		using (IServiceScope scope = provider.CreateScope())
		{
			await scope.ServiceProvider.GetRequiredService<IMasterService>().Acknowledge(new AcknowledgeRequest
			{
				MasterDatasetKey = "master", LocalDatasetKey = "local", InstalledRevision = 1
			});
		}
		Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(original));
		Assert.That(await _store.Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(1));
		Assert.That(await _store.Scalar("SELECT SyncRevision FROM LocalRegistration WHERE LocalDatasetKey = 'local'"), Is.EqualTo(1));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	[Test]
	public void CloseRejectsPersistentRootAndTemporaryRootItself()
	{
		using ServiceProvider provider = _store.CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterStorageOperation storage = scope.ServiceProvider.GetRequiredService<IMasterStorageOperation>();
		Assert.Throws<ArgumentException>(() => storage.Close(_store.DatasetDirectory));
		Assert.Throws<ArgumentException>(() => storage.Close(_store.TempRoot));
		Assert.That(File.Exists(_store.DatabasePath), Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DownloadUsesSuppliedFilenameAndCreatesParentDirectory(bool snapshot)
	{
		_store.Snapshot(0);
		using ServiceProvider provider = _store.CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterStorageOperation storage = scope.ServiceProvider.GetRequiredService<IMasterStorageOperation>();
		string directory = Path.Combine(_store.TempRoot, "Download_custom");
		string destinationFilePath = Path.Combine(directory, "custom.db");
		if (snapshot)
		{
			await storage.DownloadSnapshot(
				"master",
				0,
				destinationFilePath);
		}
		else
		{
			await storage.DownloadMaster(
				"master",
				destinationFilePath,
				null);
		}
		Assert.That(await File.ReadAllBytesAsync(destinationFilePath), Is.EqualTo(await File.ReadAllBytesAsync(_store.DatabasePath)));
		Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
		storage.Close(directory);
		Assert.That(Directory.Exists(directory), Is.False);
	}

	private static SynchronizeRequest Request(string syncKey)
	{
		SyncContainer container = new() { FormatVersion = SyncContainer.CurrentFormatVersion, SchemaVersion = 1, MasterDatasetKey = "master", LocalDatasetKey = "local" };
		container.Batches.Add(new SyncBatch { SyncKey = syncKey, CreatedAt = DateTime.UtcNow, Priority = ConflictPriority.Local });
		return new SynchronizeRequest { Container = container };
	}
}
