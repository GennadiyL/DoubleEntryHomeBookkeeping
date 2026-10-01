namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Supplies a report definition and the current device timezone for calculation.
/// Used when the owner runs or previews a report.
/// TimeZoneKey identifies the UI timezone, never the backend host default.
/// The adapter must resolve or reject an unsupported timezone key.
/// Date boundaries and calendar grouping use that same timezone.
/// The exact cross-platform timezone identifier mapping remains to be selected.
/// Calculation uses current classifications and Confirmed transactions.
/// Running a report never rewrites its saved definition.
/// </summary>
public record CalculateReport
{
	public required ReportDefinition Definition { get; set; }
	public required string TimeZoneKey { get; set; }
}
