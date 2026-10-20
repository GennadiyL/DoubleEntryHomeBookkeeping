using Business.Models.Enums;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Defines synchronization metadata for an independently tracked entity.
/// EditRevision = null - New local creation not yet captured in an outgoing batch.
/// EditRevision = 0 - Creation durably captured before sending; its accepted Master revision is unknown. Retain zero after a failed or uncertain attempt.
/// EditRevision > 0 - Last known Master revision of the entity content. Local edits preserve this value; it does not prove that the row has no pending changes.
/// DeleteRevision = null - Alive.
/// DeleteRevision = 0 - Locally soft-deleted. Deletion sets the Content flag until captured.
/// DeleteRevision > 0 - Master-assigned deletion revision. Master retains the marker until all relevant non-expired Local copies acknowledge that revision or later.
/// ModificationType.None = 0 - No subsequent edits awaiting capture. New creations and durable queued batches can still require synchronization.
/// ModificationType.Content = 1 - Business content changed or the row was soft-deleted. Includes names, amounts, references and favorites; excludes independent catalog ordering.
/// ModificationType.Order = 2 - Independent catalog order changed. This flag alone must not overwrite business content.
/// ModificationType.Content | ModificationType.Order = 3 - Both content and catalog order changed; preserve both meanings.
/// Local edits accumulate flags; durable capture clears only the captured flags, and later edits set them again.
/// Capture changes a new live row's EditRevision from null to zero while the saved batch retains creation semantics.
/// Content conflicts compare the captured revision with Master; configured priority applies subject to the same-Local exception and business validity.
/// A never-captured creation deleted locally can be physically removed during capture without an outgoing change.
/// Earlier physical removal of submitted deletions requires complete durable replay data, reference integrity and crash-safe coordination; a failed call alone never justifies discarding deletion evidence.
/// Transaction and template entries use parent tracking; their entry positions belong to parent content, not the independent Order flag.
/// </summary>
public interface ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
}
