using Business.Models.Constants;

namespace Master.Impl.Operations.MasterStorage;

/// <summary>
/// Defines object names and working directory names for a Master dataset.
/// Dataset identity appears in the directory rather than the database filename.
/// Storage object names use forward slashes for both storage providers.
/// Local paths are composed separately under the configured temporary root.
/// The main database name is also the shared storage lock identity.
/// Revisions identify completed standalone snapshots.
/// This helper performs no filesystem or storage operations.
/// Unsafe dataset keys are rejected before any path is constructed.
/// </summary>
internal static class MasterStorageNames
{
	public static string Directory(string masterDatasetKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		if (masterDatasetKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
			masterDatasetKey.Contains('/') ||
			masterDatasetKey.Contains('\\') ||
			masterDatasetKey.Contains(':'))
		{
			throw new ArgumentException("The dataset key must be a filename component.", nameof(masterDatasetKey));
		}
		return StorageValues.MasterDirectoryName(masterDatasetKey);
	}

	public static string MasterRelativePath(string masterDatasetKey) => $"{Directory(masterDatasetKey)}/{StorageValues.MasterFileName}";

	public static string SnapshotRelativePath(string masterDatasetKey, long revision) =>
		$"{Directory(masterDatasetKey)}/{SnapshotFile(revision)}";

	public static string SnapshotFile(long revision) => StorageValues.SnapshotFileName(revision);
}
