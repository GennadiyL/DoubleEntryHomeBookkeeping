namespace Business.Contracts.Services.Trees;

/// <summary>
/// Describes a live group for hierarchy selection and the group editor.
/// ParentId and ParentName identify and label its same-type parent.
/// The root has its own identity as ParentId and appears once in tree results.
/// IsRoot is derived from the fixed root identity, not editable persistent state.
/// Order controls placement among sibling groups independently of element positions.
/// Name, Description and IsFavorite supply common display values; roots are never favorites.
/// Contains detached values without persistence navigation or synchronization metadata.
/// Changes are saved through a separate mutation operation subject to root protection.
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
