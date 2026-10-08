namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Applies a user checkbox toggle to a complete tree subtree.
/// Checked rows become unchecked; unchecked or partial rows become checked.
/// Descendants receive the same explicit value, including collapsed groups.
/// Ancestors are recalculated from all immediate child states.
/// Uniform children produce that value; mixed or partial children produce null.
/// A single-child parent therefore follows its child's state.
/// Independent roots and unrelated sibling subtrees remain unchanged.
/// This helper changes UI state only and does not persist report filters.
/// </summary>
internal static class TreeGridCheckState
{
	public static void Toggle(TreeGridNode node)
	{
		bool isChecked = node.IsChecked != true;
		Stack<TreeGridNode> pending = new();
		pending.Push(node);
		while (pending.TryPop(out TreeGridNode? current))
		{
			current.IsChecked = isChecked;
			foreach (TreeGridNode child in current.Children)
			{
				pending.Push(child);
			}
		}
		for (TreeGridNode? parent = node.Parent; parent is not null; parent = parent.Parent)
		{
			bool? state = parent.Children[0].IsChecked;
			foreach (TreeGridNode child in parent.Children)
			{
				if (child.IsChecked != state)
				{
					state = null;
					break;
				}
			}
			parent.IsChecked = state;
		}
	}
}
