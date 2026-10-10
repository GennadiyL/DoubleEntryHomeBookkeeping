namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds user-authored appearance values for the desktop application.
/// Font size and row height use logical pixels and follow Windows scaling.
/// Colors use six hexadecimal RGB digits prefixed by a hash.
/// Values are loaded once from the portable UI settings file.
/// Validation rejects missing or unusable values before startup.
/// These preferences are independent of accounting precision and business rules.
/// This object does not read, write or restore configuration files.
/// Controls consume the validated settings when applying their appearance.
/// </summary>
public sealed class UiAppearanceSettings
{
	public required double FontSize { get; init; }
	public required double RowHeight { get; init; }
	public required string BackgroundStartColor { get; init; }
	public required string BackgroundEndColor { get; init; }
	public required string SelectionBorderColor { get; init; }
}
