using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Contracts;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Installs a prepared Candidate as the Local database.
/// Completes SQLite journaling before either file is moved.
/// Rejects installation while either unit has an active transaction.
/// Factory close operations release connections and invalidate cached units.
/// Atomic file replacement preserves the old database if replacement fails.
/// Cancellation is observed before the final synchronous replacement.
/// The caller excludes Local editing throughout preparation and installation.
/// Acknowledgement and pending-batch decisions belong to synchronization.
/// </summary>
internal sealed class InstallCandidateCommand : IInstallCandidateCommand
{
	private readonly IUnitOfWorkFactory _factory;
	private readonly IFileService _files;

	public InstallCandidateCommand(
		IUnitOfWorkFactory factory,
		IFileService files)
	{
		_factory = factory;
		_files = files;
	}

	public async Task Install(CancellationToken cancellationToken = default)
	{
		DatabaseFacade local = ((IEntityFrameworkUnitOfWork)_factory.CreateLocal()).Database;
		DatabaseFacade candidate = ((IEntityFrameworkUnitOfWork)_factory.CreateCandidate()).Database;
		string localPath = local.GetDbConnection().DataSource;
		string candidatePath = candidate.GetDbConnection().DataSource;
		await CompleteJournal(candidate, cancellationToken);
		await CompleteJournal(local, cancellationToken);
		await _factory.CloseCandidate();
		await _factory.CloseLocal();
		cancellationToken.ThrowIfCancellationRequested();
		_files.Replace(candidatePath, localPath, null!);
	}

	private static async Task CompleteJournal(DatabaseFacade database, CancellationToken cancellationToken)
	{
		if (database.CurrentTransaction is not null)
		{
			throw new InvalidOperationException("Candidate installation requires completed transactions.");
		}
		await database.OpenConnectionAsync(cancellationToken);
		await using System.Data.Common.DbCommand command = database.GetDbConnection().CreateCommand();
		command.CommandText = "PRAGMA journal_mode=DELETE;";
		if (!string.Equals((string?)await command.ExecuteScalarAsync(cancellationToken), "delete", StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("The database journal could not be completed before installation.");
		}
	}
}
