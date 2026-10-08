namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Identifies the operation requested by a tree drag gesture.
/// Reordering stays inside one parent's group or element sequence.
/// Moving changes the destination parent without combining nodes.
/// Merging combines a source group into a destination group.
/// The host owns confirmation, persistence and model updates.
/// Undefined does not represent an executable operation.
/// Values are independent of business services and entity types.
/// Gesture permissions are configured on the owning TreeGrid.
/// </summary>
public enum TreeGridDropOperation
{
	Undefined = 0,
	Reorder = 1,
	Move = 2,
	Merge = 3
}
