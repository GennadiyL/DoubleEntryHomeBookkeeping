namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Describes the pointer position relative to a target row.
/// Before and After identify sibling insertion boundaries.
/// Inside identifies the central region of a group row.
/// Element centers do not accept move or merge operations.
/// The drop planner combines position with node relationships.
/// This value describes a gesture rather than a stored order.
/// Undefined cannot produce a valid operation request.
/// Hosts receive the resulting operation through drop events.
/// </summary>
public enum TreeGridDropPosition
{
	Undefined = 0,
	Before = 1,
	Inside = 2,
	After = 3
}
