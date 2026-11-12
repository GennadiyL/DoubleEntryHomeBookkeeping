using Business.Models.Enums;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Defines synchronization metadata for an independently tracked entity.
/// EditRevision = null - New local creation not yet captured in an outgoing batch.
/// EditRevision = 0 - Creation durably captured before sending; its accepted Master revision is unknown. Retain zero after a failed or uncertain attempt.
/// EditRevision > 0 - Last known Master revision of the entity content. Local edits preserve this value; it does not prove that the row has no pending changes.
/// DeleteRevision = null - Alive.
/// DeleteRevision = 0 - Locally soft-deleted. This row must be captured even when modification flags are None.
/// DeleteRevision > 0 - Master-assigned deletion revision. Master retains the marker until all registered Local copies acknowledge that revision or later.
/// ModificationType.None = 0 - No subsequent edits awaiting capture. New creations, deletion markers and durable queued batches can still require synchronization.
/// ModificationType.Content = 1 - Business content changed. Includes names, amounts, references and favorites; excludes independent ordering and uncaptured deletion.
/// ModificationType.Order = 2 - Independent catalog order changed. This flag alone must not overwrite business content.
/// ModificationType.Content | ModificationType.Order = 3 - Both content and catalog order changed; preserve both meanings.
/// Local edits accumulate flags; durable capture clears only the captured flags, and later edits set them again.
/// Capture changes surviving new rows from null EditRevision to zero; deleted creations are captured with null wire revision and physically removed.
/// Content conflicts compare the captured revision with Master; configured priority applies subject to the same-Local exception and business validity.
/// Capture saves immutable JSON and hard-deletes Local deleted rows in the same transaction.
/// Restore is available before capture only; accepted snapshots supply any later Master-restored state.
/// Transaction and template entries use parent tracking; their entry positions belong to parent content, not the independent Order flag.
/// </summary>
public interface ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
}
