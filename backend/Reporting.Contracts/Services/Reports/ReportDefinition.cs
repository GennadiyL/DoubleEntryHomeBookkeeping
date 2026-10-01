namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Describes report selection, date bounds and grouping.
/// Used by the report editor and calculation operation.
/// Each classification dimension has independent selection overrides.
/// Optional date endpoints include their complete device-local days.
/// CurrencyFirst selects an outer currency grouping.
/// GroupBy determines the remaining grouping dimension.
/// The definition contains instructions rather than cached calculation results.
/// Its persistent JSON format remains a separate TRD decision.
/// </summary>
public record ReportDefinition
{
	public required ReportSelection Category { get; set; }
	public required ReportSelection Project { get; set; }
	public required ReportSelection Correspondent { get; set; }
	public DateOnly? From { get; set; }
	public DateOnly? To { get; set; }
	public bool CurrencyFirst { get; set; }
	public ReportGroupBy GroupBy { get; set; }
}
