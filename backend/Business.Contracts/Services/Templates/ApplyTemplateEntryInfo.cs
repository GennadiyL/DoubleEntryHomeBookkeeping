namespace Business.Contracts.Services.Templates;

/// <summary>
/// Describes an entry prepared by applying a template.
/// The account and amount come from the template.
/// Rate is selected for the prepared transaction date.
/// Entry order follows the containing collection.
/// No persistent identity is assigned to this entry.
/// The user may change it in the transaction editor.
/// Normal aggregate validation applies when saved.
/// Discarding it does not change the template.
/// </summary>
public record ApplyTemplateEntryInfo
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
