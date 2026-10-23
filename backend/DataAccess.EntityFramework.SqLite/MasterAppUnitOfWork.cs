using System.Data;
using DataAccess.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Implements the Master database unit of work using the shared EF behavior.
/// Receives MasterAppDbContext directly through its typed constructor.
/// Repository resolution uses the same key within the current scope.
/// All repository changes participate in this context's transactions.
/// Synchronization can bind the unused context to a prepared local file before opening it.
/// SQLite backups contain committed data independently of the source WAL file.
/// The other database role has independent repositories and tracking state.
/// The SQLite module registers this provider-specific implementation.
/// This class does not create containers or own application configuration.
/// The dependency scope manages the context lifetime.
/// </summary>
internal sealed class MasterAppUnitOfWork : AppUnitOfWork, IMasterAppUnitOfWork
{
	private readonly ISharedContext _sharedContext;

	public MasterAppUnitOfWork(
		ISharedContext sharedContext,
		MasterAppDbContext context,
		IServiceProvider serviceProvider)
		: base(context, serviceProvider, typeof(IMasterAppUnitOfWork))
	{
		_sharedContext = sharedContext;
	}

	public async Task OpenSession(string storagePath, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
		cancellationToken.ThrowIfCancellationRequested();
		EnsureUnusedContext();
		SqliteConnection connection = (SqliteConnection)Context.Database.GetDbConnection();
		SqliteConnectionStringBuilder builder = new(connection.ConnectionString) { DataSource = Path.GetFullPath(storagePath), Mode = SqliteOpenMode.ReadWrite, Pooling = false };
		connection.ConnectionString = builder.ToString();
		await Context.Database.OpenConnectionAsync(cancellationToken);
	}

	public async Task CreateSnapshot(string snapshotPath, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		cancellationToken.ThrowIfCancellationRequested();
		EnsureCommittedDatabase();
		if (_sharedContext.FileService.Exists(snapshotPath))
		{
			throw new IOException("The database snapshot destination already exists.");
		}
		SqliteConnectionStringBuilder builder = new() { DataSource = Path.GetFullPath(snapshotPath), Pooling = false };
		await using SqliteConnection destination = new(builder.ToString());
		await destination.OpenAsync(cancellationToken);
		((SqliteConnection)Context.Database.GetDbConnection()).BackupDatabase(destination);
		cancellationToken.ThrowIfCancellationRequested();
	}

	public Task CloseSession() => Context.DisposeAsync().AsTask();

	private void EnsureUnusedContext()
	{
		if (Context.Database.GetDbConnection().State != ConnectionState.Closed ||
		    Context.Database.CurrentTransaction is not null || Context.ChangeTracker.Entries().Any())
		{
			throw new InvalidOperationException("The Master database path must be configured before using the context.");
		}
	}

	private void EnsureCommittedDatabase()
	{
		if (Context.Database.GetDbConnection().State != ConnectionState.Open ||
		    Context.Database.CurrentTransaction is not null || Context.ChangeTracker.HasChanges())
		{
			throw new InvalidOperationException("A snapshot requires an open Master database with all changes committed.");
		}
	}
}
