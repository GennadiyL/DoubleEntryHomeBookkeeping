using NSubstitute;
using Shared.Contracts.Config;
using Business.Models.Entities.Synchronizations;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Master.Impl;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Impl;
using NUnit.Framework;

namespace UnitTests.Master;

/// <summary>
/// Owns isolated persistent and temporary roots for Master integration tests.
/// Seeds a real SQLite database using the production schema scripts.
/// Uses the production factory, storage processor and Master registrations.
/// Test setup can open persistent data explicitly without using a working copy.
/// Each service invocation receives a separate dependency scope.
/// Revision snapshots are standalone database files in the persistent dataset directory.
/// SQL helpers open short-lived connections so publication can replace the main file.
/// Disposal removes only the generated test root after providers have been disposed.
/// </summary>
internal sealed class MasterTestStore : IDisposable
{
	public string Root { get; } = Directory.CreateTempSubdirectory("dehb-master-tests-").FullName;
	public string StorageRoot => Path.Combine(Root, "storage");
	public string TempRoot => Path.Combine(Root, "temp");
	public string DatasetDirectory => Path.Combine(StorageRoot, "Master_master");
	public string DatabasePath => Path.Combine(DatasetDirectory, "master.db");

	public async Task Initialize(long revision = 0)
	{
		Directory.CreateDirectory(DatasetDirectory);
		Directory.CreateDirectory(TempRoot);
		await using (SqliteConnection connection = new($"Data Source={DatabasePath};Pooling=False"))
		{
			await connection.OpenAsync();
			foreach (string name in new[] { "001-schema.sql", "002-initial-data.sql" })
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", name));
				await command.ExecuteNonQueryAsync();
			}
		}
		using ServiceProvider provider = CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterUnitOfWork unit = OpenMaster(scope.ServiceProvider);
		var config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.MasterDatasetKey = "master";
		config.CurrentRevision = revision;
		unit.SystemConfigRepo.Update(config);
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "local" });
		await unit.SaveChanges();
	}

	public ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
	{
		ServiceCollection services = new();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["DataAccess:LocalDirectory"] = Root,
			["fileStorage:Root"] = StorageRoot
		}).Build();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		IConfigurationService directories = Substitute.For<IConfigurationService>();
		directories.GetTempDirectory().Returns(TempRoot);
		services.AddSingleton(directories);
		services.AddEntityFrameworkSqLiteModule();
		services.AddMasterModule();
		configure?.Invoke(services);
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	public IMasterUnitOfWork OpenMaster(IServiceProvider provider) =>
		provider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master", DatasetDirectory);

	public async Task Execute(string sql)
	{
		await using SqliteConnection connection = new($"Data Source={DatabasePath};Pooling=False");
		await connection.OpenAsync();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = sql;
		await command.ExecuteNonQueryAsync();
	}

	public async Task<long> Scalar(string sql)
	{
		await using SqliteConnection connection = new($"Data Source={DatabasePath};Pooling=False");
		await connection.OpenAsync();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = sql;
		return (long)(await command.ExecuteScalarAsync())!;
	}

	public void Snapshot(long revision) => File.Copy(DatabasePath, Path.Combine(DatasetDirectory, $"snapshot_{revision}.db"), true);

	public void Dispose() => Directory.Delete(Root, true);
}
