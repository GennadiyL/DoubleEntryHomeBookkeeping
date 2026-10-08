namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Lets a host reject a structurally valid drag destination.
/// Raised during drag feedback and again immediately before a drop.
/// Validation must be quick and must not modify or save data.
/// Cancel blocks the operation and displays the forbidden cursor.
/// Reason optionally explains rejection in the drag caption.
/// The request contains UI nodes whose Data may identify business models.
/// Confirmation belongs in DropRequested rather than validation.
/// Permission checks may differ between main views and popups.
/// </summary>
public sealed class TreeGridDropValidationEventArgs : EventArgs
{
	public TreeGridDropRequest Request { get; set; } = null!;
	public bool Cancel { get; set; }
	public string Reason { get; set; } = string.Empty;
}
