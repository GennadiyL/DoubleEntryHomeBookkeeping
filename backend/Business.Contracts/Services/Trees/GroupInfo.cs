namespace Business.Contracts.Services.Trees;

/// <summary>
/// Describes one group in a flat catalog hierarchy.
/// ParentId and Order allow the UI to construct the tree.
/// Created by the service for presentation and editing.
/// Contains values rather than persistent entity references.
/// Identifiers refer to existing bookkeeping records.
/// Does not expose synchronization revisions or modification flags.
/// Reading this record does not save changes.
/// Persistence remains the responsibility of the corresponding mutation operation.
/// </summary>
public record GroupInfo
{
	public Guid Id { get; set; }
	public Guid ParentId { get; set; }
	public string ParentName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
	public bool IsRoot { get; set; }
}
