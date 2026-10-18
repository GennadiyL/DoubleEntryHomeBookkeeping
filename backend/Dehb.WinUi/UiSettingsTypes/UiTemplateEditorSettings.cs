namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds dimensions for the Template editor and its entry columns.
/// Inherited width and height determine the fixed popup size.
/// AmountWidth and CurrencyWidth are integer logical pixel widths.
/// Entry rows and their headers share these configured dimensions.
/// Account selectors fill the space remaining after other controls.
/// Layout also reserves padding, gaps, buttons and scrollbar space.
/// The settings loader validates column widths before startup.
/// This model contains configuration only and performs no UI operations.
/// </summary>
public sealed class UiTemplateEditorSettings : UiEditorPopupSettings
{
	public required int AmountWidth { get; init; }
	public required int CurrencyWidth { get; init; }
}
