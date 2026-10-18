namespace Dehb.WinUi.UiSettingsTypes;

/// <summary>
/// Holds Ledger column widths and account-filter control dimensions.
/// Columns retains the standard named grid-column configuration.
/// DateWidth sizes the Ledger calendar date picker.
/// RangeWidth sizes the period navigation combobox.
/// AccountWidth sizes the read-only account-filter textbox.
/// All dimensions use logical pixels and follow display scaling.
/// The settings loader validates required positive dimensions.
/// This model does not load files or manage filter state.
/// </summary>
public sealed class UiTransactionGridSettings : UiGridSettings
{
	public required int DateWidth { get; init; }
	public required int RangeWidth { get; init; }
	public required int AccountWidth { get; init; }
}
