namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds named column widths for one desktop list.
/// Dictionary keys identify columns rather than their display positions.
/// Width values use logical pixels and follow Windows scaling.
/// The enclosing UI settings validate the expected column names.
/// Every expected column must have a finite positive width.
/// These preferences are local and do not synchronize with bookkeeping data.
/// This object contains no persistence or layout application behavior.
/// Grid controls consume these values when their layout is configured.
/// </summary>
public sealed class UiGridSettings
{
	public required Dictionary<string, double> Columns { get; init; }
}
