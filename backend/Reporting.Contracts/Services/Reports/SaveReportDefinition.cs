namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Saves a report definition after an explicit editor action.
/// A null Id creates a report and a supplied Id selects an existing report.
/// Name is validated using the saved-report naming rules.
/// Definition represents the selection being explicitly saved.
/// Selection saves recompute minimal override lists.
/// Read and calculation operations never perform this normalization.
/// Rename-only preservation details remain a TRD decision.
/// The command contains no synchronization revision or calculated results.
/// </summary>
public record SaveReportDefinition
{
	public Guid? Id { get; set; }
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public required ReportDefinition Definition { get; set; }
}
