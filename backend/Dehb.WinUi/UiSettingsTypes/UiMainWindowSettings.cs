namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds user-authored settings specific to the main application window.
/// The sidebar has its own solid background, independent of dialog gradients.
/// Sidebar item height uses logical pixels and follows Windows scaling.
/// Minimum window dimensions also use logical pixels.
/// Saved window placement remains separate from these minimum dimensions.
/// Values are loaded once at startup from the portable UI settings file.
/// The settings loader validates colors and positive dimensions.
/// This object does not load, save or restore window placement.
/// </summary>
public sealed class UiMainWindowSettings
{
	public required string SideBarBackColor { get; init; }
	public required double SideBarFontSize { get; init; }
	public required double SideBarItemHeight { get; init; }
	public required double MinWidth { get; init; }
	public required double MinHeight { get; init; }
}
