namespace Business.Contracts.Services.Templates;

/// <summary>
/// Describes an account and amount within a template.
/// The containing collection supplies the entry order.
/// Rates are looked up when the template is applied.
/// No independent entry identity is exposed.
/// Template edits replace the complete collection.
/// The collection can describe an unbalanced template.
/// The service returns values without persistent navigation objects.
/// Persistence occurs only through template Add or Update.
/// </summary>
public record TemplateEntryInfo
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
}
