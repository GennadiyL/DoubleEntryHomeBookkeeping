using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Adapter.Azure.Storage;

/// <summary>
/// Holds state and owns temporary files for one Master database working copy.
/// Retains the processor and object name selected by the adapter at opening.
/// The caller-owned storage lock is retained without transferring ownership.
/// The adapter reads and updates lifecycle state while holding the session gate.
/// Disposal uses the same gate so files cannot be removed during publication.
/// Repeated disposal is safe and does not delete caller-owned snapshot files.
/// Cleanup failures are logged without hiding an earlier processing exception.
/// The caller closes database connections before disposal and releases its lock afterward.
/// </summary>
internal sealed class BlobMasterStorageSession : IMasterStorageSession
{
	private readonly ISharedContext _sharedContext;
	private readonly DirectoryInfo _directory;

	internal BlobMasterStorageSession(
		ISharedContext sharedContext,
		IStorageProcessor storage,
		string objectName,
		IStorageLock access,
		DirectoryInfo directory)
	{
		_sharedContext = sharedContext;
		_directory = directory;
		Storage = storage;
		ObjectName = objectName;
		Access = access;
		StoragePath = Path.Combine(directory.FullName, "master.db");
	}

	public string StoragePath { get; }
	internal IStorageProcessor Storage { get; }
	internal string ObjectName { get; }
	internal IStorageLock Access { get; }
	internal SemaphoreSlim Gate { get; } = new(1, 1);
	internal bool IsDisposed { get; private set; }
	internal bool IsFaulted { get; set; }

	public async ValueTask DisposeAsync()
	{
		await Gate.WaitAsync();
		try
		{
			if (IsDisposed)
			{
				return;
			}
			IsDisposed = true;
			DeleteTemporaryDirectory();
		}
		finally
		{
			Gate.Release();
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
