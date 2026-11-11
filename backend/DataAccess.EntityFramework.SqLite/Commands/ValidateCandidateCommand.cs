using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Validates the downloaded Candidate database before preparation.
/// Resolves the Candidate context through the scoped unit-of-work factory.
/// Checks SQLite integrity and foreign-key consistency.
/// Compares stored columns with the expected relational schema.
/// Validation reads database state without changing persisted data.
/// Dataset identity and revision checks remain with the download operation.
/// This command neither prepares Local state nor installs database files.
/// The factory retains ownership of the context and its connection.
/// </summary>
internal sealed class ValidateCandidateCommand : IValidateCandidateCommand
{
	private readonly IUnitOfWorkFactory _factory;

	public ValidateCandidateCommand(IUnitOfWorkFactory factory) => _factory = factory;

	public async Task Validate(CancellationToken cancellationToken = default)
	{
		IEntityFrameworkUnitOfWork unit = (IEntityFrameworkUnitOfWork)_factory.CreateCandidate();
		DbContext context = unit.Database.GetService<ICurrentDbContext>().Context;
		await unit.Database.OpenConnectionAsync(cancellationToken);
		SqliteConnection connection = (SqliteConnection)unit.Database.GetDbConnection();
		await using SqliteCommand check = connection.CreateCommand();
		check.CommandText = "PRAGMA quick_check;";
		if (!Equals(await check.ExecuteScalarAsync(cancellationToken), "ok"))
		{
			throw new InvalidDataException("The downloaded database failed its integrity check.");
		}
		check.CommandText = "PRAGMA foreign_key_check;";
		await using (SqliteDataReader reader = await check.ExecuteReaderAsync(cancellationToken))
		{
			if (await reader.ReadAsync(cancellationToken))
			{
				throw new InvalidDataException("The downloaded database has invalid foreign keys.");
			}
		}
		foreach (ITable table in context.Model.GetRelationalModel().Tables)
		{
			check.CommandText = $"PRAGMA table_info(\"{table.Name}\");";
			Dictionary<string, (string Type, bool Required)> columns = new();
			await using (SqliteDataReader reader = await check.ExecuteReaderAsync(cancellationToken))
			{
				while (await reader.ReadAsync(cancellationToken))
				{
					columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt64(3) != 0));
				}
			}
			if (columns.Count != table.Columns.Count() ||
				table.Columns.Any(column =>
					!columns.TryGetValue(column.Name, out (string Type, bool Required) stored) ||
					stored.Type != column.StoreType ||
					stored.Required == column.IsNullable))
			{
				throw new InvalidDataException($"The downloaded table '{table.Name}' has an incompatible schema.");
			}
		}
	}

}
