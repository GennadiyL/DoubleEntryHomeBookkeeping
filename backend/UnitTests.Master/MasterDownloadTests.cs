using Business.Models.Exceptions;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Contracts.Storage;
using NSubstitute;

namespace UnitTests.Master;

/// <summary>
/// Exercises memory-backed downloads from published Master snapshots.
/// Uses real SQLite metadata and the production Local storage processor.
/// Download reads the current published revision rather than choosing orphan snapshots.
/// Parallel requests have isolated directories and do not acquire the Master write lock.
/// Returned streams outlive their dependency scopes and temporary files.
/// Validation failures and interrupted transfers must remove working directories.
/// Missing published snapshots fail rather than silently substituting different data.
/// Each test owns its persistent files and disposes its providers before cleanup.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the temporary store.")]
[TestFixture(Category = "Local")]
public sealed class MasterDownloadTests
{
	private MasterTestStore _store = null!;

	[SetUp]
	public async Task SetUp()
	{
		_store = new MasterTestStore();
		await _store.Initialize(7);
		_store.Snapshot(7);
	}

	[TearDown]
	public void TearDown() => _store.Dispose();

	[Test]
	public async Task DownloadReturnsMemoryAndCleansDirectoryBeforeReturning()
	{
		using ServiceProvider provider = _store.CreateProvider();
		DownloadResponse result = await Download(provider, Request());
		using Stream content = result.Content;
		Assert.That(content, Is.TypeOf<MemoryStream>());
		Assert.That(content.Position, Is.Zero);
		Assert.That(result.Revision, Is.EqualTo(7));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
		byte[] expected = await File.ReadAllBytesAsync(Path.Combine(_store.DatasetDirectory, "snapshot_7.db"));
		Assert.That(((MemoryStream)content).ToArray(), Is.EqualTo(expected));
		await _store.Execute("UPDATE SystemConfig SET CurrentRevision = 8");
		Assert.That(((MemoryStream)content).ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public async Task ParallelDownloadsRemainIndependentWhileMasterIsLocked()
	{
		using ServiceProvider provider = _store.CreateProvider();
		await using IStorageLock access = (await provider.GetRequiredService<IStorageLockFactory>().TryAcquire("Master_master/master.db"))!;
		DownloadResponse[] results = await Task.WhenAll(Download(provider, Request()), Download(provider, Request()));
		using Stream first = results[0].Content;
		using Stream second = results[1].Content;
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(((MemoryStream)first).ToArray(), Is.EqualTo(((MemoryStream)second).ToArray()));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	[Test]
	public async Task OrphanSnapshotDoesNotAdvanceDownloadRevision()
	{
		await File.WriteAllBytesAsync(Path.Combine(_store.DatasetDirectory, "snapshot_8.db"), [9]);
		using ServiceProvider provider = _store.CreateProvider();
		DownloadResponse result = await Download(provider, Request());
		using Stream content = result.Content;
		Assert.That(result.Revision, Is.EqualTo(7));
		Assert.That(content.Length, Is.GreaterThan(1));
	}

	[TestCase(0)]
	[TestCase(7)]
	public async Task MinimumRevisionRemainsALowerBound(long minimum)
	{
		using ServiceProvider provider = _store.CreateProvider();
		DownloadResponse result = await Download(provider, Request() with { MinimumRevision = minimum });
		using Stream content = result.Content;
		Assert.That(result.Revision, Is.EqualTo(7));
	}

	[TestCase("", "local", 0)]
	[TestCase("master", "", 0)]
	[TestCase("master", "local", -1)]
	[TestCase("master", "unknown", 0)]
	[TestCase("master", "local", 8)]
	public void InvalidDownloadIsRejected(string masterKey, string localKey, long minimum)
	{
		using ServiceProvider provider = _store.CreateProvider();
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Download(provider,
			new DownloadRequest { MasterDatasetKey = masterKey, LocalDatasetKey = localKey, MinimumRevision = minimum }));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	[Test]
	public void MissingPublishedSnapshotFailsAndCleansDirectory()
	{
		File.Delete(Path.Combine(_store.DatasetDirectory, "snapshot_7.db"));
		using ServiceProvider provider = _store.CreateProvider();
		Assert.ThrowsAsync<FileNotFoundException>(() => Download(provider, Request()));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	[Test]
	public async Task InitialRevisionZeroCanBeDownloadedBeforeFirstBatch()
	{
		await _store.Execute("UPDATE SystemConfig SET CurrentRevision = 0");
		using ServiceProvider provider = _store.CreateProvider();
		DownloadResponse result = await Download(provider, Request());
		using Stream content = result.Content;
		Assert.That(result.Revision, Is.Zero);
		Assert.That(content.Length, Is.GreaterThan(0));
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedSnapshotReadClosesFileAndCleansDirectory(bool cancelled)
	{
		using ServiceProvider baseline = _store.CreateProvider();
		using IServiceScope baselineScope = baseline.CreateScope();
		IStorageProcessor real = baselineScope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
		IStorageProcessor storage = Substitute.For<IStorageProcessor>();
		IStorageProcessorFactory factory = Substitute.For<IStorageProcessorFactory>();
		factory.CreateProcessor().Returns(storage);
		Stream? temporaryFile = null;
		storage.Read(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IStorageLock?>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			string name = call.ArgAt<string>(0);
			Stream destination = call.ArgAt<Stream>(1);
			if (name.EndsWith("snapshot_7.db", StringComparison.Ordinal))
			{
				temporaryFile = destination;
				Assert.That(call.ArgAt<IStorageLock?>(2), Is.Null);
				await destination.WriteAsync(new byte[] { 1 });
				if (cancelled)
				{
					throw new OperationCanceledException();
				}
				throw new IOException("Interrupted snapshot.");
			}
			await real.Read(name, destination, null, call.ArgAt<CancellationToken>(3));
		});
		using ServiceProvider provider = _store.CreateProvider(services => services.AddSingleton(factory));
		Assert.CatchAsync(() => Download(provider, Request()));
		Assert.That(temporaryFile, Is.TypeOf<FileStream>());
		Assert.That(temporaryFile!.CanWrite, Is.False);
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
	}

	private static DownloadRequest Request() => new() { MasterDatasetKey = "master", LocalDatasetKey = "local" };

	private static async Task<DownloadResponse> Download(ServiceProvider provider, DownloadRequest request)
	{
		using IServiceScope scope = provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Download(request);
	}
}
