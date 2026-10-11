namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds the portable application's user-authored UI configuration.
/// The version identifies the supported JSON settings structure.
/// Main-window settings describe the sidebar and window limits.
/// Appearance settings provide shared fonts, row height, dialog button dimensions and colors.
/// Currency and rate settings hold their named column widths.
/// UiSettingsLoader handles deserialization and validation separately.
/// This model performs no file access, validation or automatic repair.
/// Business settings and saved window placement are separate models.
/// </summary>
public sealed class UiSettings
{
	public required int Version { get; init; }
	public required UiMainWindowSettings MainWindow { get; init; }
	public required UiAppearanceSettings Appearance { get; init; }
	public required UiGridSettings AccountTreeGrid { get; init; }
	public required UiGridSettings CurrencyGrid { get; init; }
	public required UiGridSettings CurrencyRateGrid { get; init; }
	public required UiEditorPopupSettings AddCurrencyEditor { get; init; }
	public required UiEditorPopupSettings EditCurrencyEditor { get; init; }
	public required UiEditorPopupSettings AddCurrencyRateEditor { get; init; }
	public required UiEditorPopupSettings EditCurrencyRateEditor { get; init; }
	public required UiEditorPopupSettings DeleteCurrencyRateEditor { get; init; }
}
