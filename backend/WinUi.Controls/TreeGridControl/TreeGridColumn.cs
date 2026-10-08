using Microsoft.UI.Xaml;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Describes an additional data column between the hierarchy and star cells.
/// Percentage widths are configured through the owning TreeGrid.
/// BindingPath is resolved against the TreeGridNode, including its Data property.
/// An optional cell template receives that same node as its content.
/// Column definitions are replaced through the Columns collection when changed.
/// The application chooses captions, formatting and domain-specific bindings.
/// This definition contains no sorting, persistence or database behavior.
/// Name and star columns are supplied by the TreeGrid itself.
/// </summary>
public sealed record TreeGridColumn
{
	public string Header { get; set; } = string.Empty;
	public string BindingPath { get; set; } = string.Empty;
	public DataTemplate? CellTemplate { get; set; }
}
