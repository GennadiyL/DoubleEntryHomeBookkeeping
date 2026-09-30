namespace Business.Models.Enums;

/// <summary>
/// Identifies local changes not yet captured in a durable outgoing batch.
/// Content marks entity changes; Order marks catalog position changes; both flags may combine.
/// None means no uncaptured flags, even when saved outgoing batches remain pending.
/// Capture clears captured flags atomically; later edits set flags for a subsequent batch.
/// Explicit numeric values preserve the meaning of stored or exchanged selections.
/// The enum describes state or preferences without executing the corresponding operations.
/// </summary>
[Flags]
public enum ModificationType
{
	None = 0,
	Content = 1,
	Order = 2
}
