namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Finds names in the complete supplied hierarchy in preorder.
/// Collapsed branches participate without changing their expansion state.
/// Matching uses a case-insensitive substring of the row name.
/// Navigation starts after the current row and wraps in either direction.
/// Without a current row, navigation starts at the corresponding end.
/// Empty queries and absent matches return no row.
/// Each request reads current names and order without a cached index.
/// Selection, revealing matches and user feedback belong to TreeGrid.
/// </summary>
internal static class TreeGridSearch
{
	public static TreeGridNode? Find(IEnumerable<TreeGridNode> roots, TreeGridNode? current, string query, bool previous)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return null;
		}
		List<TreeGridNode> rows = new();
		Stack<TreeGridNode> pending = new(roots.Reverse());
		while (pending.TryPop(out TreeGridNode? node))
		{
			rows.Add(node);
			for (int i = node.Children.Count - 1; i >= 0; i--)
			{
				pending.Push(node.Children[i]);
			}
		}
		int start = current is null ? -1 : rows.IndexOf(current);
		if (start < 0 && previous)
		{
			start = rows.Count;
		}
		for (int offset = 1; offset <= rows.Count; offset++)
		{
			int index = (start + (previous ? -offset : offset) + rows.Count) % rows.Count;
			if (rows[index].Name.Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				return rows[index];
			}
		}
		return null;
	}
}
