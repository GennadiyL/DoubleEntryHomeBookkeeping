namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Selects the grouping dimension for calculated report rows.
/// Used by report definitions and report editors.
/// Undefined is unset and must not select a grouping.
/// Classification groupings use current account classifications.
/// Calendar groupings use the current device timezone.
/// Weeks run from Monday through Sunday.
/// Currency-first grouping is a separate definition setting.
/// Explicit values do not define the saved JSON encoding.
/// </summary>
public enum ReportGroupBy
{
	Undefined = 0,
	Category = 1,
	Project = 2,
	Correspondent = 3,
	Day = 4,
	Week = 5,
	Month = 6,
	Year = 7
}
