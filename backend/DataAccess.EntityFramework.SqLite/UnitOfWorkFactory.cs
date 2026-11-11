using Business.Models.Constants;
using Shared.Contracts.Config;
using DataAccess.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Creates and caches database units within one API dependency scope.
/// Local is cached once; Master is cached by its working database path.
/// Snapshots are cached by dataset key and exact revision.
/// Each cache entry owns one directly instantiated AppDbContext.
/// Repositories are created lazily using the current scoped service provider.
/// Synchronous and asynchronous disposal release every owned context.
/// Disposal does not commit changes or delete database files.
/// Instances and their contexts are intended for sequential operation use.
/// </summary>
internal sealed class UnitOfWorkFactory : IUnitOfWorkFactory, IDisposable, IAsyncDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly string _localDirectory;
	private readonly IConfigurationService _configurationService;
	private readonly Dictionary<(Type Role, string Path, long Revision), (IAppUnitOfWork Unit, AppDbContext Context)> _units = new();
	private bool _disposed;

	public UnitOfWorkFactory(
		IServiceProvider serviceProvider,
		IConfiguration configuration,
		IConfigurationService configurationService)
	{
		_serviceProvider = serviceProvider;
		_localDirectory = GetDirectory(configuration, StorageValues.LocalDirectoryConfigurationKey);
		_configurationService = configurationService;
	}

	public ILocalUnitOfWork CreateLocal() =>
		GetOrCreate(
			typeof(ILocalUnitOfWork),
			0,
			Path.Combine(_localDirectory, StorageValues.LocalFileName),
			(context, provider) => new LocalUnitOfWork(context, provider));

	public IMasterUnitOfWork CreateMaster(string masterDatasetKey) =>
		CreateMaster(masterDatasetKey, Path.Combine(_configurationService.GetTempDirectory(), StorageValues.MasterDirectoryName(masterDatasetKey)));

	public IMasterUnitOfWork CreateMaster(string masterDatasetKey, string directoryPath)
	{
		ValidateKey(masterDatasetKey);
		return GetOrCreate(
			typeof(IMasterUnitOfWork),
			0,
			Path.Combine(Path.GetFullPath(directoryPath), StorageValues.MasterFileName),
			(context, provider) => new MasterUnitOfWork(
				context,
				provider,
				masterDatasetKey));
	}

	public ISnapshotUnitOfWork CreateSnapshot(string masterDatasetKey, long revision)
	{
		ValidateKey(masterDatasetKey);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		return GetOrCreate(
			typeof(ISnapshotUnitOfWork),
			revision,
			Path.Combine(
				_configurationService.GetTempDirectory(),
				StorageValues.MasterDirectoryName(masterDatasetKey),
				StorageValues.SnapshotFileName(revision)),
			(context, provider) => new SnapshotUnitOfWork(
				context,
				provider,
				masterDatasetKey,
				revision));
	}

	public ICandidateUnitOfWork CreateCandidate() =>
		GetOrCreate(
			typeof(ICandidateUnitOfWork),
			0,
			Path.Combine(_localDirectory, StorageValues.CandidateFileName),
			(context, provider) => new CandidateUnitOfWork(context, provider));

	public Task CloseLocal() =>
		Close(
			typeof(ILocalUnitOfWork),
			Path.Combine(_localDirectory, StorageValues.LocalFileName),
			0);

	public Task CloseCandidate() =>
		Close(
			typeof(ICandidateUnitOfWork),
			Path.Combine(_localDirectory, StorageValues.CandidateFileName),
			0);

	public Task CloseMaster(string masterDatasetKey) =>
		CloseMaster(masterDatasetKey, Path.Combine(_configurationService.GetTempDirectory(), StorageValues.MasterDirectoryName(masterDatasetKey)));

	public Task CloseMaster(string masterDatasetKey, string directoryPath)
	{
		ValidateKey(masterDatasetKey);
		return Close(
			typeof(IMasterUnitOfWork),
			Path.Combine(Path.GetFullPath(directoryPath), StorageValues.MasterFileName),
			0);
	}

	public Task CloseSnapshot(string masterDatasetKey, long revision)
	{
		ValidateKey(masterDatasetKey);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		return Close(
			typeof(ISnapshotUnitOfWork),
			Path.Combine(
				_configurationService.GetTempDirectory(),
				StorageValues.MasterDirectoryName(masterDatasetKey),
				StorageValues.SnapshotFileName(revision)),
			revision);
	}

	private async Task Close(
		Type role,
		string path,
		long revision)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_units.TryGetValue((role, path, revision), out (IAppUnitOfWork Unit, AppDbContext Context) entry))
		{
			await entry.Unit.DisposeAsync();
			_units.Remove((role, path, revision));
		}
	}

	private TUnit GetOrCreate<TUnit>(
		Type role,
		long revision,
		string path,
		Func<AppDbContext, IServiceProvider, TUnit> create) where TUnit : IAppUnitOfWork
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_units.TryGetValue((role, path, revision), out (IAppUnitOfWork Unit, AppDbContext Context) cached))
		{
			return (TUnit)cached.Unit;
		}
		string connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWrite,
			Pooling = false,
			ForeignKeys = true
		}.ToString();
		DbContextOptions<AppDbContext> options =
			new DbContextOptionsBuilder<AppDbContext>()
				.UseSqlite(connectionString)
				.ReplaceService<IModelCustomizer, SqLiteAppModelCustomizer>()
				.Options;
		AppDbContext context = new(options);
		try
		{
			TUnit unit = create(context, _serviceProvider);
			_units.Add((role, path, revision), (unit, context));
			return unit;
		}
		catch
		{
			context.Dispose();
			throw;
		}
	}

	private static string GetDirectory(IConfiguration configuration, string key)
	{
		string? directory = configuration[key];
		if (string.IsNullOrWhiteSpace(directory))
		{
			throw new InvalidOperationException($"Configuration '{key}' is required.");
		}
		return Path.GetFullPath(directory);
	}

	private static void ValidateKey(string masterDatasetKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		if (masterDatasetKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
			masterDatasetKey.Contains('/') ||
			masterDatasetKey.Contains('\\') ||
			masterDatasetKey.Contains(':'))
		{
			throw new ArgumentException("The dataset key must be a valid filename component.", nameof(masterDatasetKey));
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		List<Exception> errors = new();
		foreach ((IAppUnitOfWork Unit, AppDbContext Context) entry in _units.Values)
		{
			try
			{
				entry.Context.Dispose();
			}
			catch (Exception exception)
			{
				errors.Add(exception);
			}
		}
		_units.Clear();
		if (errors.Count != 0)
		{
			throw new AggregateException(errors);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		List<Exception> errors = new();
		foreach ((IAppUnitOfWork Unit, AppDbContext Context) entry in _units.Values)
		{
			try
			{
				await entry.Context.DisposeAsync();
			}
			catch (Exception exception)
			{
				errors.Add(exception);
			}
		}
		_units.Clear();
		if (errors.Count != 0)
		{
			throw new AggregateException(errors);
		}
	}
}
