namespace Business.Contracts.Services.Trees;

/// <summary>
/// Describes one category for catalog browsing and selection.
/// GroupId connects the element to its catalog group.
/// Created by the service for presentation and editing.
/// Contains values rather than persistent entity references.
/// Identifiers refer to existing bookkeeping records.
/// Does not expose synchronization revisions or modification flags.
/// Reading this record does not save changes.
/// Persistence remains the responsibility of the corresponding mutation operation.
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
