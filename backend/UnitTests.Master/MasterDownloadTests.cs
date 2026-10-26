using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Master;

/// <summary>
/// Verifies standalone Master downloads through the public service boundary.
/// Uses the production SQLite backup and host-selected storage registrations.
/// Local and Remote modes share a local storage root without Azure dependencies.
/// Snapshot bytes must retain their revision after subsequent Master changes.
/// Returned streams survive service-scope disposal and release temporary files.
/// Invalid requests and busy ownership must prevent snapshot creation.
/// Controlled persistence failures verify cancellation and cleanup behavior.
/// Each test owns its database and removes its temporary files during teardown.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class MasterDownloadTests
{
	private DirectoryInfo _directory = null!;
	private string DatabasePath => Path.Combine(_directory.FullName, "Master_master.db");

	[SetUp]
	public async Task SetUp()
	{
		_directory = Directory.CreateTempSubdirectory("dehb-master-download-tests-");
		await using SqliteConnection connection = new($"Data Source={DatabasePath};Pooling=False");
		await connection.OpenAsync();
		foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql" })
		{
			await using SqliteCommand command = connection.CreateCommand();
			command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
			await command.ExecuteNonQueryAsync();
		}
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.MasterDatasetKey = "master";
		config.CurrentRevision = 7;
		unit.SystemConfigRepo.Update(config);
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "local" });
		await unit.SaveChanges();
	}

	[TearDown]
	public void TearDown()
	{
		if (_directory is not null && Directory.Exists(_directory.FullName))
		{
			_directory.Delete(true);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SnapshotSurvivesScopeAndMasterChangesAndDeletesOnDispose(bool remote)
	{
		using ServiceProvider provider = BuildProvider(remote);
		DownloadInfo result = await Download(provider, Request());
		await using Stream content = result.Content;
		string snapshotPath = ((FileStream)content).Name;
		Assert.That(result.Revision, Is.EqualTo(7));
		Assert.That(content.CanWrite, Is.False);
		await using (IStorageLock? access = await provider.GetRequiredService<IStorageLockFactory>().TryAcquire("Master_master.db"))
		{
			Assert.That(access, Is.Not.Null);
		}
		using (IServiceScope scope = provider.CreateScope())
		{
			IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = 8;
			unit.SystemConfigRepo.Update(config);
			await unit.SaveChanges();
		}
		string copyPath = Path.Combine(_directory.FullName, "download.db");
		await using (FileStream copy = File.Create(copyPath))
		{
			await content.CopyToAsync(copy);
		}
		await using (SqliteConnection copy = new($"Data Source={copyPath};Pooling=False"))
		{
			await copy.OpenAsync();
			await using SqliteCommand command = copy.CreateCommand();
			command.CommandText = "SELECT CurrentRevision FROM SystemConfig";
			Assert.That((long)(await command.ExecuteScalarAsync())!, Is.EqualTo(result.Revision));
			command.CommandText = "PRAGMA integrity_check";
			Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo("ok"));
		}
		await content.DisposeAsync();
		Assert.That(File.Exists(snapshotPath), Is.False);
		Assert.That(File.Exists(DatabasePath), Is.True);
	}

	[TestCase("", "local", 0)]
	[TestCase("master", "", 0)]
	[TestCase("master", "local", -1)]
	[TestCase("wrong", "local", 0)]
	[TestCase("master", "unknown", 0)]
	[TestCase("master", "local", 8)]
	public void InvalidDownloadIsRejected(string masterKey, string localKey, long minimum)
	{
		using ServiceProvider provider = BuildProvider();
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Download(provider,
			new DownloadRequest { MasterDatasetKey = masterKey, LocalDatasetKey = localKey, MinimumRevision = minimum }));
	}

	[TestCase(0)]
	[TestCase(7)]
	public async Task MinimumRevisionIsALowerBound(long minimum)
	{
		using ServiceProvider provider = BuildProvider();
		DownloadInfo result = await Download(provider, Request() with { MinimumRevision = minimum });
		await using Stream content = result.Content;
		Assert.That(result.Revision, Is.EqualTo(7));
	}

	[Test]
	public async Task BusyMasterIsRejectedAndCanBeRetriedAfterRelease()
	{
		using ServiceProvider provider = BuildProvider();
		IStorageLock access = (await provider.GetRequiredService<IStorageLockFactory>().TryAcquire("Master_master.db"))!;
		await using (access)
		{
			Assert.ThrowsAsync<MasterBusyException>(() => Download(provider, Request()));
		}
		DownloadInfo result = await Download(provider, Request());
		await result.Content.DisposeAsync();
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public async Task CancelledOrFailedSnapshotIsDeletedAndDatabaseClosed(int failure)
	{
		using CancellationTokenSource cancellation = new();
		IMasterAppUnitOfWork unit = Substitute.For<IMasterAppUnitOfWork>();
		unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>())
			.Returns(new List<SystemConfig> { new() { MasterDatasetKey = "master", CurrentRevision = 7 } });
		unit.LocalRegistrationRepo.GetByLocalDatasetKey("local", Arg.Any<CancellationToken>())
			.Returns(new LocalRegistration { LocalDatasetKey = "local" });
		string? snapshotPath = null;
		unit.CreateSnapshot(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			snapshotPath = call.ArgAt<string>(0);
			await File.WriteAllTextAsync(snapshotPath, "partial");
			if (failure != 0)
			{
				await cancellation.CancelAsync();
				return;
			}
			throw new IOException("Snapshot failed.");
		});
		IStorageLockFactory? locks = null;
		if (failure == 2)
		{
			locks = Substitute.For<IStorageLockFactory>();
			IStorageLock handle = Substitute.For<IStorageLock>();
			handle.CancellationToken.Returns(cancellation.Token);
			locks.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns(handle);
		}
		using ServiceProvider provider = BuildProvider(unit: unit, locks: locks);
		if (failure != 0)
		{
			Assert.CatchAsync<OperationCanceledException>(() => Download(provider, Request(), failure == 2 ? CancellationToken.None : cancellation.Token));
		}
		else
		{
			Assert.ThrowsAsync<IOException>(() => Download(provider, Request()));
		}
		Assert.That(snapshotPath, Is.Not.Null);
		Assert.That(File.Exists(snapshotPath), Is.False);
		await unit.Received(1).CloseSession();
		await using IStorageLock? access = await provider.GetRequiredService<IStorageLockFactory>().TryAcquire("Master_master.db");
		Assert.That(access, Is.Not.Null);
	}

	[Test]
	public void PreCancelledDownloadDoesNotAccessDatabase()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		using ServiceProvider provider = BuildProvider();
		Assert.CatchAsync<OperationCanceledException>(() => Download(provider, Request(), cancellation.Token));
	}

	private ServiceProvider BuildProvider(bool remote = false, IMasterAppUnitOfWork? unit = null, IStorageLockFactory? locks = null)
	{
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:DehbMasterSqLite"] = $"Data Source={DatabasePath};Pooling=False",
			["fileStorage:Root"] = _directory.FullName
		}).Build();
		ServiceCollection services = new();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddMasterDataAccessSqLiteModule(configuration);
		services.AddMasterModule();
		if (remote)
		{
			services.AddRemoteMasterStorage();
		}
		else
		{
			services.AddLocalMasterStorage(_ => DatabasePath);
		}
		if (unit is not null)
		{
			services.AddSingleton(unit);
		}
		if (locks is not null)
		{
			services.AddSingleton(locks);
		}
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private static DownloadRequest Request() =>
		new() { MasterDatasetKey = "master", LocalDatasetKey = "local", MinimumRevision = 7 };

	private static async Task<DownloadInfo> Download(ServiceProvider provider, DownloadRequest request, CancellationToken cancellationToken = default)
	{
		using IServiceScope scope = provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Download(request, cancellationToken);
	}
}
