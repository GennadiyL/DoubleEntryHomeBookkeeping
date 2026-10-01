namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Describes explicit selection overrides for one classification dimension.
/// Used for category, project and correspondent report selection.
/// Group identities preserve selected hierarchy intent.
/// Element identities represent explicit inclusions and exclusions.
/// IncludeUnassigned controls accounts without that classification.
/// Missing identities remain until an explicit save removes obsolete references.
/// Evaluation uses the current hierarchy and current account classifications.
/// Reading or calculating must not normalize the saved selection.
/// </summary>
public record ReportSelection
{
	public List<Guid> IncludedGroupIds { get; } = new();
	public List<Guid> ExcludedGroupIds { get; } = new();
	public List<Guid> IncludedElementIds { get; } = new();
	public List<Guid> ExcludedElementIds { get; } = new();
	public bool IncludeUnassigned { get; set; }
}
