namespace Business.Models.Enums;

/// <summary>
/// Identifies subsequent local edits not yet captured in a durable outgoing batch.
/// None is zero and is the initial value for every new tracked entity.
/// New creations are detected by null EditRevision, without requiring either flag.
/// Content marks content edits, favorite changes and soft deletion.
/// Order marks catalog position changes independently of content; both flags may combine.
/// Content edits preserve Order, and order-only edits preserve Content without adding it.
/// Entry positions belong to parent aggregate content rather than independent Order flags.
/// Batch capture clears captured bits atomically; later edits set them for a later batch.
/// None does not imply synchronization is complete while creations or outgoing batches remain.
/// Flags do not assign or increment Master revisions.
/// </summary>
[Flags]
public enum ModificationType
{
	None = 0,
	Content = 1,
	Order = 2
}
