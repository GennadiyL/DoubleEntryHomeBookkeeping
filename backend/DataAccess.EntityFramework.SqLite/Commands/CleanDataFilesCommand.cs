using DataAccess.Contracts.Commands;
using Shared.Contracts;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Removes a closed SQLite database and its associated data files.
/// Encapsulates WAL, shared-memory and rollback-journal filename conventions.
/// Callers identify only the primary database file.
/// Cleanup uses the injected filesystem service.
/// Missing files are ignored to support retries and interrupted downloads.
/// Unrelated files and the containing directory are preserved.
/// Connections must be closed by the caller before cleanup.
/// Filesystem failures propagate to the owning operation.
/// </summary>
internal sealed class CleanDataFilesCommand : ICleanDataFilesCommand
{
	private const string WalSuffix = "-wal";
	private const string SharedMemorySuffix = "-shm";
	private const string JournalSuffix = "-journal";
	private readonly IFileService _fileService;

	public CleanDataFilesCommand(IFileService fileService) => _fileService = fileService;

	public void CleanAll(string databaseFilePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(databaseFilePath);
		foreach (string suffix in new[] { "", WalSuffix, SharedMemorySuffix, JournalSuffix })
		{
			_fileService.Delete(databaseFilePath + suffix);
		}
	}
}
