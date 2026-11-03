using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Master.Impl.Operations.MasterStorage;

/// <summary>
/// Copies Master databases between persistent storage and temporary working files.
/// The configured processor selects filesystem or Azure storage without changing the workflow.
/// The operation retains dependencies only and creates no session objects.
/// Synchronize and Acknowledge retain the Master lock throughout publication and cleanup.
/// Download uses a unique directory and reads only published database state.
/// Standalone SQLite backups include committed data independently of WAL files.
/// MasterService orders snapshot upload before the main database upload.
/// Cleanup owns temporary directories only and never deletes published snapshots.
/// </summary>
internal sealed class MasterStorageOperation : IMasterStorageOperation
{
	private readonly ISharedContext _sharedContext;
	private readonly IStorageProcessorFactory _storageProcessorFactory;

	public MasterStorageOperation(
		ISharedContext sharedContext,
		IStorageProcessorFactory storageProcessorFactory)
	{
		_sharedContext = sharedContext;
		_storageProcessorFactory = storageProcessorFactory;
	}

	public async Task DownloadMaster(
		string masterDatasetKey,
		string destinationFilePath,
		IStorageLock? access,
		CancellationToken cancellationToken = default)
	{
		string directoryPath = Path.GetDirectoryName(Path.GetFullPath(destinationFilePath))!;
		ValidateDirectory(directoryPath);
		string objectName = MasterStorageNames.MasterRelativePath(masterDatasetKey);
		cancellationToken.ThrowIfCancellationRequested();
		access?.CancellationToken.ThrowIfCancellationRequested();
		_sharedContext.DirectoryService.CreateDirectory(directoryPath);
		foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
		{
			_sharedContext.FileService.Delete(destinationFilePath + suffix);
		}
		await using FileStream destination = _sharedContext.FileService.Open(destinationFilePath, new FileStreamOptions
		{
			Mode = FileMode.Create,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
		await _storageProcessorFactory.CreateProcessor().Read(
			objectName,
			destination,
			access,
			cancellationToken);
	}

	public async Task DownloadSnapshot(
		string masterDatasetKey,
		long revision,
		string destinationFilePath,
		CancellationToken cancellationToken = default)
	{
		string directoryPath = Path.GetDirectoryName(Path.GetFullPath(destinationFilePath))!;
		ValidateDirectory(directoryPath);
		string objectName = MasterStorageNames.SnapshotRelativePath(masterDatasetKey, revision);
		cancellationToken.ThrowIfCancellationRequested();
		_sharedContext.DirectoryService.CreateDirectory(directoryPath);
		await using FileStream destination = _sharedContext.FileService.Open(destinationFilePath, new FileStreamOptions
		{
			Mode = FileMode.Create,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
		await _storageProcessorFactory.CreateProcessor().Read(
			objectName,
			destination,
			null,
			cancellationToken);
	}

	public async Task UploadMaster(
		string masterDatasetKey,
		string sourceFilePath,
		IStorageLock access,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		access.CancellationToken.ThrowIfCancellationRequested();
		await using FileStream source = OpenSource(sourceFilePath);
		await _storageProcessorFactory.CreateProcessor().Write(
			MasterStorageNames.MasterRelativePath(masterDatasetKey),
			source,
			true,
			access,
			cancellationToken);
	}

	public async Task UploadSnapshot(
		string masterDatasetKey,
		long revision,
		string sourceFilePath,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using FileStream snapshot = OpenSource(sourceFilePath);
		await _storageProcessorFactory.CreateProcessor().Write(
			MasterStorageNames.SnapshotRelativePath(masterDatasetKey, revision),
			snapshot,
			true,
			null,
			cancellationToken);
	}

	public void Close(string directoryPath)
	{
		ValidateDirectory(directoryPath);
		try
		{
			if (_sharedContext.DirectoryService.Exists(directoryPath))
			{
				_sharedContext.DirectoryService.Delete(directoryPath, true);
			}
		}
		catch (IOException exception)
		{
			_sharedContext.LogService.Error("Master temporary directory cleanup failed.", exception);
		}
		catch (UnauthorizedAccessException exception)
		{
			_sharedContext.LogService.Error("Master temporary directory cleanup failed.", exception);
		}
	}

	private FileStream OpenSource(string sourceFilePath)
	{
		ValidateDirectory(Path.GetDirectoryName(Path.GetFullPath(sourceFilePath))!);
		return _sharedContext.FileService.Open(sourceFilePath, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
	}

	private void ValidateDirectory(string directoryPath)
	{
		string relative = Path.GetRelativePath(_sharedContext.ConfigurationService.GetTempDirectory(), Path.GetFullPath(directoryPath));
		if (Path.IsPathRooted(relative) ||
			relative == "." ||
			relative == ".." ||
			relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
			relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
		{
			throw new ArgumentException("The working directory must be below the scoped temporary root.", nameof(directoryPath));
		}
	}
}
