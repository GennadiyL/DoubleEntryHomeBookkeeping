namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Returns a saved report definition for browsing and editing.
/// Id identifies the persistent report without exposing its model.
/// Name is the saved display name.
/// Definition contains selection and grouping instructions.
/// Missing referenced identities must be retained on reads.
/// No calculated rows are persisted in this projection.
/// Synchronization revisions are not editable API fields.
/// The projection does not prescribe the underlying JSON representation.
/// </summary>
public record ReportInfo
{
	public Guid Id { get; set; }
	public Guid GroupId { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public required ReportDefinition Definition { get; set; }
}
