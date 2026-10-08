namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Describes a validated drag operation without applying changes.
/// Source and Target retain the same instances used by the tree.
/// DestinationParent identifies the collection affected by the drop.
/// InsertIndex is zero-based within the source kind's sibling sequence.
/// The index excludes Source and includes siblings hidden by a filter.
/// Move requests append within the destination's corresponding sequence.
/// Merge requests leave child conflict handling to the host.
/// Hosts save successfully before changing observable UI collections.
/// </summary>
public sealed record TreeGridDropRequest
{
	public TreeGridNode Source { get; set; } = null!;
	public TreeGridNode Target { get; set; } = null!;
	public TreeGridNode DestinationParent { get; set; } = null!;
	public TreeGridDropOperation Operation { get; set; }
	public int InsertIndex { get; set; }
}
