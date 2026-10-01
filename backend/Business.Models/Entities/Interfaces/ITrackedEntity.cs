using Business.Models.Enums;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Defines synchronization metadata for an independently tracked entity.
/// New local entities have null edit and delete revisions and ModificationType.None.
/// EditRevision null means never submitted; zero means submitted with an unknown Master revision.
/// Positive revisions come from Master; ordinary local edits preserve the received revision.
/// DeleteRevision null means alive; zero marks local soft deletion; positive means accepted deletion.
/// Local content and order edits accumulate their respective flags without clearing other bits.
/// Durable batch capture clears captured flags atomically; later edits set them again.
/// Pending synchronization also includes new entities and saved outgoing batches, even with no flags.
/// Only delta preparation may remove a never-submitted deletion before synchronization.
/// Transaction and template entries use their parent tracking instead of this contract.
/// </summary>
public interface ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
}
