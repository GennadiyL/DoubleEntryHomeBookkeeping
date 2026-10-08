namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Resolves a pointer target into a domain-independent tree operation.
/// Root dragging, self drops and ancestor cycles are rejected.
/// Reordering is limited to siblings of the same node kind.
/// A filtered insertion is resolved against the complete sibling sequence.
/// Shift over a group requests merging only when explicitly allowed.
/// No hierarchy, selection or expansion state is changed by planning.
/// Business validation is performed separately by the host event.
/// The same planner is used for feedback and final drop validation.
/// </summary>
internal static class TreeGridDropPlanner
{
	public static TreeGridDropRequest? Create(TreeGridNode source, TreeGridNode target,
		TreeGridDropPosition position, bool shift, bool allowMerge, IEnumerable<TreeGridNode> visibleRows)
	{
		if (source.Parent is null || ReferenceEquals(source, target))
		{
			return null;
		}
		for (TreeGridNode? ancestor = target; ancestor is not null; ancestor = ancestor.Parent)
		{
			if (ReferenceEquals(ancestor, source))
			{
				return null;
			}
		}
		if (position == TreeGridDropPosition.Inside)
		{
			if (!target.IsGroup)
			{
				return null;
			}
			TreeGridDropOperation operation = TreeGridDropOperation.Move;
			if (shift && source.IsGroup)
			{
				if (!allowMerge)
				{
					return null;
				}
				operation = TreeGridDropOperation.Merge;
			}
			if (operation == TreeGridDropOperation.Move && ReferenceEquals(source.Parent, target))
			{
				return null;
			}
			return new TreeGridDropRequest
			{
				Source = source, Target = target, DestinationParent = target, Operation = operation,
				InsertIndex = target.Children.Count(child => child.IsGroup == source.IsGroup)
			};
		}
		if (position is not (TreeGridDropPosition.Before or TreeGridDropPosition.After)
			|| source.IsGroup != target.IsGroup || !ReferenceEquals(source.Parent, target.Parent))
		{
			return null;
		}
		List<TreeGridNode> siblings = source.Parent.Children
			.Where(child => child.IsGroup == source.IsGroup && !ReferenceEquals(child, source)).ToList();
		TreeGridNode? preceding = target;
		if (position == TreeGridDropPosition.Before)
		{
			preceding = visibleRows.TakeWhile(row => !ReferenceEquals(row, target))
				.LastOrDefault(row => row.IsGroup == source.IsGroup && ReferenceEquals(row.Parent, source.Parent)
					&& !ReferenceEquals(row, source));
		}
		int index = preceding is null ? 0 : siblings.IndexOf(preceding) + 1;
		int currentIndex = source.Parent.Children.Where(child => child.IsGroup == source.IsGroup).ToList().IndexOf(source);
		if (index == currentIndex)
		{
			return null;
		}
		return new TreeGridDropRequest
		{
			Source = source, Target = target, DestinationParent = source.Parent,
			Operation = TreeGridDropOperation.Reorder, InsertIndex = index
		};
	}
}
