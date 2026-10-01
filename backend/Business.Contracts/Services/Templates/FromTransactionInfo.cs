namespace Business.Contracts.Services.Templates;

/// <summary>
/// Contains an unsaved template prepared from a transaction.
/// Accounts, amounts and description are copied from the source.
/// Entry order is preserved in a stable collection.
/// Transaction timestamps, rates and state are not template inputs.
/// The user supplies the template name and group before saving.
/// No persistent template or entry identity is assigned here.
/// The normal template Add operation performs validation and persistence.
/// Canceling leaves the source transaction unchanged.
/// </summary>
public record FromTransactionInfo
{
	public string? Description { get; set; }
	public List<TemplateEntryInfo> Entries { get; } = new();
}
