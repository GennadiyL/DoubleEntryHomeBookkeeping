namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Supplies validated column percentages to the hosting application.
/// Raised synchronously when the user clicks Apply in the Columns dialog.
/// Widths follow the name, additional columns and star display order.
/// The host can save these values under its own view or picker key.
/// ErrorMessage rejects Apply and keeps the dialog open for another attempt.
/// The existing layout remains unchanged when persistence is rejected.
/// Programmatic restoration does not raise this event or save settings.
/// This contract contains no file paths or application-specific identities.
/// </summary>
public sealed class TreeGridColumnWidthsEventArgs : EventArgs
{
	public IReadOnlyList<double> Widths { get; }
	public string? ErrorMessage { get; set; }

	public TreeGridColumnWidthsEventArgs(IEnumerable<double> widths)
	{
		Widths = Array.AsReadOnly(widths.ToArray());
	}
}
