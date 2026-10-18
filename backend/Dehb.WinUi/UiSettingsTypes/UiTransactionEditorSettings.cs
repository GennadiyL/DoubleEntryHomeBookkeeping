namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds the fixed Transaction editor size and entry column widths.
/// Inherited dimensions describe the popup in logical pixels.
/// DateWidth and TimeWidth size the first-row date and time controls.
/// Amount and currency widths size their individual columns.
/// RateWidth sizes the rate editor, excluding its restore button.
/// BaseAmountWidth sizes the calculated base amount column.
/// Account selectors fill the remaining entry row space.
/// The loader validates these required dimensions before startup.
/// This model performs no UI operations or financial calculations.
/// </summary>
public sealed class UiTransactionEditorSettings : UiEditorPopupSettings
{
	public required int DateWidth { get; init; }
	public required int TimeWidth { get; init; }
	public required int AmountWidth { get; init; }
	public required int CurrencyWidth { get; init; }
	public required int RateWidth { get; init; }
	public required int BaseAmountWidth { get; init; }
}
