namespace Synchronization.Contracts.Services.Diagnostics;

/// <summary>
/// Summarizes received business changes for one entity family.
/// Used in the latest synchronization report.
/// Created counts newly received records.
/// Updated counts received content changes.
/// Deleted counts received deletions.
/// Counts are nonnegative and zero counts may be omitted in display.
/// Upload-only work and unchanged snapshot rows are not received changes.
/// Ordering messages belong to the report message collection.
/// </summary>
public record SyncReportItemInfo
{
	public SyncEntityType EntityType { get; set; }
	public int Created { get; set; }
	public int Updated { get; set; }
	public int Deleted { get; set; }
}
