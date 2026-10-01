namespace Business.Contracts.Services.Templates;

/// <summary>
/// Contains an unsaved transaction prepared from a template.
/// DateTime is set to the current UTC time.
/// Description, accounts and amounts are copied from the template.
/// Entry rates are selected for the transaction date.
/// Entry order is preserved in a stable collection.
/// No persistent identity or sync state is included.
/// The user saves through the normal transaction Add operation.
/// Canceling changes neither transactions nor the source template.
/// </summary>
public record ApplyTemplateInfo
{
	public DateTime DateTime { get; set; }
	public string? Description { get; set; }
	public List<ApplyTemplateEntryInfo> Entries { get; } = new();
}
