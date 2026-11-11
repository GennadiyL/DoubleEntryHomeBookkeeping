using Business.Models.Constants;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Creates a standalone SQLite snapshot of the committed Master database.
/// Resolves the Master unit through the scoped caching factory.
/// Uses its EF database facade to access the source connection.
/// Reads the current revision from the Master configuration repository.
/// Places the revision-named snapshot beside the main Master database.
/// SQLite backup copies the database without table-specific mapping.
/// The command owns only the destination connection.
/// The caller owns publication and removal of the working snapshot.
/// </summary>
internal sealed class CreateSnapshotCommand : ICreateSnapshotCommand
{
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;

	public CreateSnapshotCommand(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

	public async Task CreateSnapshot(string masterDatasetKey, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IMasterUnitOfWork unitOfWork = _unitOfWorkFactory.CreateMaster(masterDatasetKey);
		DatabaseFacade database = ((IEntityFrameworkUnitOfWork)unitOfWork).Database;
		if (database.CurrentTransaction is not null)
		{
			throw new InvalidOperationException("Snapshot creation requires committed Master changes.");
		}

		long revision = (await unitOfWork.SystemConfigRepo.GetAll(cancellationToken)).Single().CurrentRevision;
		SqliteConnection source = (SqliteConnection)database.GetDbConnection();
		string snapshotPath = Path.Combine(
			Path.GetDirectoryName(Path.GetFullPath(source.DataSource))!,
			StorageValues.SnapshotFileName(revision));
		await database.OpenConnectionAsync(cancellationToken);
		await using SqliteConnection destination = new(new SqliteConnectionStringBuilder
		{
			DataSource = snapshotPath,
			Pooling = false
		}.ToString());
		await destination.OpenAsync(cancellationToken);
		source.BackupDatabase(destination);
		cancellationToken.ThrowIfCancellationRequested();
	}
}
