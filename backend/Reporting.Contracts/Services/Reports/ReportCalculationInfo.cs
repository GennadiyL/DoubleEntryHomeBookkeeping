namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Contains calculated report rows and user-visible warnings.
/// Used by the report result screen.
/// Rows reflect current data and current classification hierarchy.
/// Warnings include an empty-selection outcome where applicable.
/// Base totals sum per-entry rounded values using stored rates.
/// Own-currency totals must not combine incompatible currencies.
/// Collections are stable and may be empty.
/// The result is transient and is never a saved report definition.
/// </summary>
public record ReportCalculationInfo
{
	public List<ReportRowInfo> Rows { get; } = new();
	public List<string> Warnings { get; } = new();
}
