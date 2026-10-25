using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Adapter.Azure.Storage;

/// <summary>
/// Owns a temporary Master database and publishes snapshots through object storage.
/// Retains the processor selected when the adapter opens this session.
/// The downloaded database path is used only by the caller's persistence resources.
/// Publication accepts a separate completed snapshot and keeps it read-locked during upload.
/// The caller-owned storage lock is forwarded unchanged to each storage operation.
/// Failed publication invalidates the session to prevent reuse after an uncertain outcome.
/// Disposal waits for publication and removes only this session's temporary directory.
/// Cleanup failures are logged without hiding an earlier processing exception.
/// The caller closes database connections before disposal and separately releases its lock.
/// </summary>
internal sealed class BlobMasterStorageSession : IMasterStorageSession
{
	private readonly ISharedContext _sharedContext;
	private readonly IStorageProcessor _storage;
	private readonly string _objectName;
	private readonly IStorageLock _access;
	private readonly DirectoryInfo _directory;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private bool _disposed;
	private bool _faulted;

	internal BlobMasterStorageSession(
		ISharedContext sharedContext,
		IStorageProcessor storage,
		string objectName,
		IStorageLock access,
		DirectoryInfo directory)
	{
		_sharedContext = sharedContext;
		_storage = storage;
		_objectName = objectName;
		_access = access;
		_directory = directory;
		StoragePath = Path.Combine(directory.FullName, "master.db");
	}

	public string StoragePath { get; }

	internal async Task Download(CancellationToken cancellationToken)
	{
		using CancellationTokenSource cancellation = LinkCancellation(cancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		await using FileStream destination = OpenDownload(_sharedContext.FileService, StoragePath);
		await _storage.Read(_objectName, destination, _access, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task Publish(string snapshotPath, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cancellation = LinkCancellation(cancellationToken);
		await _gate.WaitAsync(cancellation.Token);
		try
		{
			EnsureUsable();
			await UploadSnapshot(snapshotPath, cancellation.Token);
		}
		catch
		{
			_faulted = true;
			throw;
		}
		finally
		{
			_gate.Release();
		}
	}

	private async Task UploadSnapshot(string snapshotPath, CancellationToken cancellationToken)
	{
		ValidateSnapshotPath(snapshotPath);
		cancellationToken.ThrowIfCancellationRequested();
		await using FileStream snapshot = OpenSnapshot(_sharedContext.FileService, snapshotPath);
		await _storage.Write(_objectName, snapshot, true, _access, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
	}

	private static FileStream OpenDownload(IFileService fileService, string path) =>
		fileService.Open(path, new FileStreamOptions
		{
			Mode = FileMode.CreateNew,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});

	private static FileStream OpenSnapshot(IFileService fileService, string path) =>
		fileService.Open(path, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			BufferSize = 81920,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});

	private void ValidateSnapshotPath(string snapshotPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (string.Equals(Path.GetFullPath(snapshotPath), StoragePath, comparison))
		{
			throw new ArgumentException("Publication requires a separate completed database snapshot.", nameof(snapshotPath));
		}
	}

	private void EnsureUsable()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_faulted)
		{
			throw new InvalidOperationException("The storage session failed. Reopen the Master database before retrying.");
		}
	}

	private CancellationTokenSource LinkCancellation(CancellationToken cancellationToken) =>
		CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _access.CancellationToken);

	public async ValueTask DisposeAsync()
	{
		await _gate.WaitAsync();
		try
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			DeleteTemporaryDirectory();
		}
		finally
		{
			_gate.Release();
		}
	}

	private void DeleteTemporaryDirectory()
	{
		try
		{
			if (_sharedContext.DirectoryService.Exists(_directory.FullName))
			{
				_sharedContext.DirectoryService.Delete(_directory.FullName, true);
			}
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			_sharedContext.LogService.Warn("Master storage temporary directory cleanup failed.", exception);
		}
	}
}
