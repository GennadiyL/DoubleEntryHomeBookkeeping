using System.Globalization;

namespace Business.Models.Constants;

/// <summary>
/// Defines database filenames and their related storage naming conventions.
/// Local and Candidate files share the configured Local directory.
/// Master files reside beneath the dataset-specific directory.
/// Snapshot filenames identify a single business revision.
/// Database filenames use lowercase independently of the current culture.
/// Provider-specific auxiliary files are outside these naming conventions.
/// Directory prefixes preserve the established storage object layout.
/// This class performs no filesystem access or database operations.
/// </summary>
public static class StorageValues
{
	public const string LocalFileName = "local.db";
	public const string MasterFileName = "master.db";
	public const string CandidateFileName = "candidate.db";
	public const string DatabaseExtension = ".db";
	public const string SnapshotPrefix = "snapshot_";
	public const string MasterDirectoryPrefix = "Master_";
	public const string DownloadDirectoryPrefix = "Download_";
	public const string LocalDirectoryConfigurationKey = "DataAccess:LocalDirectory";

	public static string SnapshotFileName(long revision) =>
		$"{SnapshotPrefix}{revision.ToString(CultureInfo.InvariantCulture)}{DatabaseExtension}";

	public static string MasterDirectoryName(string masterDatasetKey) => $"{MasterDirectoryPrefix}{masterDatasetKey}";

	public static string DownloadDirectoryName(Guid requestId) => $"{DownloadDirectoryPrefix}{requestId:N}";
}
