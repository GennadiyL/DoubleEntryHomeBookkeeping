namespace Business.Contracts.Services.Trees;

/// <summary>
/// Describes a lightweight element in any of the five catalog trees.
/// GroupId and GroupName identify and label its containing group.
/// Name, Description and IsFavorite supply the common visible tree columns.
/// Order controls position in the separate element sequence, not a numeric display column.
/// Also supplies full edit values for Category, Correspondent and Project.
/// Account classifications and template entries require their separate full edit records.
/// Contains detached values without persistent navigation or synchronization metadata.
/// Changing this record does not persist changes.
/// </summary>
public record ElementInfo
{
	public Guid Id { get; set; }
	public Guid GroupId { get; set; }
	public string GroupName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
}
