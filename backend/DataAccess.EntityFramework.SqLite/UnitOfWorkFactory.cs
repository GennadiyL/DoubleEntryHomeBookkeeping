using System.Globalization;
using DataAccess.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Creates and caches database units within one API dependency scope.
/// Local is cached once; Master is cached by dataset key.
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
	private readonly string _masterDirectory;
	private readonly string _snapshotDirectory;
	private readonly Dictionary<(Type Role, string Key, long Revision), (IAppUnitOfWork Unit, AppDbContext Context)> _units = new();
	private bool _disposed;

	public UnitOfWorkFactory(IServiceProvider serviceProvider, IConfiguration configuration)
	{
		_serviceProvider = serviceProvider;
		_localDirectory = GetDirectory(configuration, "LocalDirectory");
		_masterDirectory = GetDirectory(configuration, "MasterDirectory");
		_snapshotDirectory = GetDirectory(configuration, "SnapshotDirectory");
	}

	public ILocalUnitOfWork CreateLocal() =>
		GetOrCreate(
			typeof(ILocalUnitOfWork),
			string.Empty,
			0,
			Path.Combine(_localDirectory, "Local.db"),
			(context, provider) => new LocalUnitOfWork(context, provider));

	public IMasterUnitOfWork CreateMaster(string masterDatasetKey)
	{
		ValidateKey(masterDatasetKey);
		return GetOrCreate(
			typeof(IMasterUnitOfWork),
			masterDatasetKey,
			0,
			Path.Combine(_masterDirectory, $"Master_{masterDatasetKey}.db"),
			(context, provider) => new MasterUnitOfWork(context, provider, masterDatasetKey));
	}

	public ISnapshotUnitOfWork CreateSnapshot(string masterDatasetKey, long revision)
	{
		ValidateKey(masterDatasetKey);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		string version = revision.ToString(CultureInfo.InvariantCulture);
		return GetOrCreate(
			typeof(ISnapshotUnitOfWork),
			masterDatasetKey,
			revision,
			Path.Combine(_snapshotDirectory, $"Master_{masterDatasetKey}_{version}.db"),
			(context, provider) => new SnapshotUnitOfWork(context, provider, masterDatasetKey, revision));
	}

	private TUnit GetOrCreate<TUnit>(
		Type role,
		string key,
		long revision,
		string path,
		Func<AppDbContext, IServiceProvider, TUnit> create) where TUnit : IAppUnitOfWork
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_units.TryGetValue((role, key, revision), out (IAppUnitOfWork Unit, AppDbContext Context) cached))
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
		DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(connectionString)
			.ReplaceService<IModelCustomizer, SqLiteAppModelCustomizer>()
			.Options;
		AppDbContext context = new(options);
		try
		{
			TUnit unit = create(context, _serviceProvider);
			_units.Add((role, key, revision), (unit, context));
			return unit;
		}
		catch
		{
			context.Dispose();
			throw;
		}
	}

	private static string GetDirectory(IConfiguration configuration, string name)
	{
		string key = $"DataAccess:{name}";
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
