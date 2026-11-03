using Shared.Contracts.Storage;

namespace Master.Impl.Operations.MasterStorage;

internal interface IMasterStorageOperation
{
	/// <summary>
	/// Copies the published Master into the supplied temporary file, creating its parent directory.
	/// </summary>
	public Task DownloadMaster(
		string masterDatasetKey,
		string destinationFilePath,
		IStorageLock? access,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Copies a published revision into the supplied temporary file without a storage lock, creating its parent directory.
	/// </summary>
	public Task DownloadSnapshot(
		string masterDatasetKey,
		long revision,
		string destinationFilePath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Uploads the supplied standalone database file as the persistent Master.
	/// </summary>
	public Task UploadMaster(
		string masterDatasetKey,
		string sourceFilePath,
		IStorageLock access,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Uploads the supplied standalone database file as the revision snapshot without a storage lock.
	/// </summary>
	public Task UploadSnapshot(
		string masterDatasetKey,
		long revision,
		string sourceFilePath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes the operation's temporary directory after database connections close.
	/// Cleanup does not release storage ownership or delete persistent objects.
	/// </summary>
	public void Close(string directoryPath);
}
