using System.Data;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using DataAccess.Contracts;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Implements the Local database unit of work using the shared EF behavior.
/// Receives LocalAppDbContext directly through its typed constructor.
/// Repository resolution uses the same key within the current scope.
/// All repository changes participate in this context's transactions.
/// The other database role has independent repositories and tracking state.
/// The SQLite module registers this provider-specific implementation.
/// This class does not create containers or own application configuration.
/// The dependency scope manages the context lifetime.
/// </summary>
internal sealed class LocalAppUnitOfWork : AppUnitOfWork, ILocalAppUnitOfWork
{
	public LocalAppUnitOfWork(
		LocalAppDbContext context,
		IServiceProvider serviceProvider)
		: base(context, serviceProvider, typeof(ILocalAppUnitOfWork))
	{
	}

	public async Task OpenSnapshot(string snapshotPath, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		if (Context.Database.GetDbConnection().State != ConnectionState.Closed || Context.ChangeTracker.Entries().Any() || Context.Database.CurrentTransaction is not null)
		{
			throw new InvalidOperationException("Snapshot preparation requires an unused Local scope.");
		}
		SqliteConnection connection = (SqliteConnection)Context.Database.GetDbConnection();
		connection.ConnectionString = new SqliteConnectionStringBuilder
		{
			DataSource = Path.GetFullPath(snapshotPath), Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = true
		}.ToString();
		await connection.OpenAsync(cancellationToken);
		await using SqliteCommand check = connection.CreateCommand();
		check.CommandText = "PRAGMA quick_check;";
		if (!Equals(await check.ExecuteScalarAsync(cancellationToken), "ok"))
		{
			throw new InvalidDataException("The downloaded database failed its integrity check.");
		}
		check.CommandText = "PRAGMA foreign_key_check;";
		await using (SqliteDataReader reader = await check.ExecuteReaderAsync(cancellationToken))
		{
			if (await reader.ReadAsync(cancellationToken)) { throw new InvalidDataException("The downloaded database has invalid foreign keys."); }
		}
		foreach (ITable table in Context.Model.GetRelationalModel().Tables)
		{
			check.CommandText = $"PRAGMA table_info(\"{table.Name}\");";
			Dictionary<string, (string Type, bool Required)> columns = new();
			await using (SqliteDataReader reader = await check.ExecuteReaderAsync(cancellationToken))
			{
				while (await reader.ReadAsync(cancellationToken)) { columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt64(3) != 0)); }
			}
			if (columns.Count != table.Columns.Count() || table.Columns.Any(column =>
				!columns.TryGetValue(column.Name, out (string Type, bool Required) stored) || stored.Type != column.StoreType || stored.Required == column.IsNullable))
			{
				throw new InvalidDataException($"The downloaded table '{table.Name}' has an incompatible schema.");
			}
		}
	}

	public async Task ReplaceLocalState(LocalConfig configuration, LocalJson pending, CancellationToken cancellationToken = default)
	{
		if (Context.Database.CurrentTransaction is null) { throw new InvalidOperationException("Local state replacement requires a transaction."); }
		await Context.Set<DataAccess.EntityFramework.Models.LocalConfig>().ExecuteDeleteAsync(cancellationToken);
		await Context.Set<DataAccess.EntityFramework.Models.LocalJson>().ExecuteDeleteAsync(cancellationToken);
		LocalConfigRepo.Add(configuration);
		LocalJsonRepo.Add(pending);
		await SaveChanges(cancellationToken);
	}

	public async Task InstallSnapshot(string snapshotPath, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		if (Context.Database.CurrentTransaction is not null || Context.ChangeTracker.Entries().Any())
		{
			throw new InvalidOperationException("Snapshot activation requires a committed, untracked Local context.");
		}
		await using SqliteConnection source = new(new SqliteConnectionStringBuilder
		{
			DataSource = Path.GetFullPath(snapshotPath), Mode = SqliteOpenMode.ReadOnly, Pooling = false
		}.ToString());
		await source.OpenAsync(cancellationToken);
		await Context.Database.OpenConnectionAsync(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		source.BackupDatabase((SqliteConnection)Context.Database.GetDbConnection());
		Context.ChangeTracker.Clear();
	}
}
