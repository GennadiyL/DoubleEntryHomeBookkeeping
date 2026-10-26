namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Returns the Master revision observed after processing the submitted container.
/// Successful completion confirms every batch was applied or already applied.
/// Revision is a lower bound for a subsequent database download.
/// Other Local copies may advance Master before that download starts.
/// An empty or entirely deduplicated container can return the current revision.
/// This result does not reserve a snapshot or issue a transfer token.
/// Download reports the actual revision represented by its returned bytes.
/// Failures propagate instead of being encoded as progress states.
/// </summary>
public record SynchronizeResponse
{
	public long Revision { get; set; }
}
