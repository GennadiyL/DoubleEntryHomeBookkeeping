namespace Master.Impl.Operations.MasterStorage;

/// <summary>
/// Defines the storage object name for a Master database.
/// Master orchestration and its storage operation use the same naming rule.
/// Shared storage components receive the resulting name without knowing dataset semantics.
/// The configured storage root or container is selected separately.
/// This helper does not access storage or acquire ownership.
/// </summary>
internal static class MasterStorageNames
{
	public static string Database(string masterDatasetKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		return $"Master_{masterDatasetKey}.db";
	}
}
