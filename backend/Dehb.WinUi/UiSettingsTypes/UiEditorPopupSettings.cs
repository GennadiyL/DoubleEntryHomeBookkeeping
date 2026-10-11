namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds the fixed dimensions of one application editor popup.
/// Width and height are integer logical pixels, scaled for the owner's DPI.
/// Each editor has its own instance in the root UI settings.
/// The loader validates required dimensions before opening the application.
/// EditorWindow applies these dimensions when creating the popup.
/// Fixed editors disable resizing, minimize and maximize commands.
/// Their field layout uses the available client width and configured margins.
/// This model performs no file access or window management.
/// </summary>
public sealed class UiEditorPopupSettings
{
	public required int Width { get; init; }
	public required int Height { get; init; }
}
