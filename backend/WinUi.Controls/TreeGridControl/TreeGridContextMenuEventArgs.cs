using Microsoft.UI.Xaml.Controls;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Provides a row and an empty menu for the hosting application.
/// TreeGrid selects the requested row before raising this event.
/// The host synchronously adds commands with its own labels and permissions.
/// Command handlers can use Node.Data to locate application models.
/// The menu belongs to this single request and is not cached for other rows.
/// An empty menu suppresses display without undoing row selection.
/// Mouse and keyboard requests use the same host integration point.
/// No domain actions or persistence are supplied by the control.
/// </summary>
public sealed class TreeGridContextMenuEventArgs : EventArgs
{
	public TreeGridNode Node { get; }
	public MenuFlyout Menu { get; } = new();

	public TreeGridContextMenuEventArgs(TreeGridNode node)
	{
		Node = node;
	}
}
