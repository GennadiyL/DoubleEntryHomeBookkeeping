namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Carries detached entity rows and relationship metadata for one batch.
/// Restoration and validation receive this data through operation parameters.
/// Rows reference the same merge records used for revision stamping and persistence.
/// RootId identifies a protected catalog root when applicable.
/// ParentType and ParentField describe the owning catalog relationship.
/// IsOrdered indicates whether catalog order normalization applies.
/// No repository callbacks, reflection accessors or business behavior are stored here.
/// Each invocation owns its dictionaries; operations retain none of this state.
/// </summary>
internal sealed record SyncEntityState
{
	public required string Type { get; init; }
	public required Type EntityType { get; init; }
	public string[] ContentFields { get; init; } = [];
	public Guid? RootId { get; init; }
	public string? ParentType { get; init; }
	public string? ParentField { get; init; }
	public bool IsOrdered { get; init; }
	public Dictionary<Guid, SyncMergeRow> Rows { get; } = new();
}
