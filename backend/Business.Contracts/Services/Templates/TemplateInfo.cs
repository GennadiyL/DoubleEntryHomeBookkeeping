namespace Business.Contracts.Services.Templates;

/// <summary>
/// Contains a saved template's complete values for the template editor.
/// GroupId and GroupName identify and label the containing template group.
/// Name and optional Description are independent values.
/// Order and IsFavorite describe catalog placement and explicit favorite selection.
/// Entries contains the complete entry list in stored zero-based Position order.
/// Entry rows include account and currency labels but no persistent entry identities.
/// Lightweight catalog trees omit these entry details.
/// The detached result contains no synchronization state and saves nothing.
/// </summary>
public record TemplateInfo
{
	public Guid Id { get; set; }
	public Guid GroupId { get; set; }
	public string GroupName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
	public List<TemplateEntryInfo> Entries { get; } = new();
}
