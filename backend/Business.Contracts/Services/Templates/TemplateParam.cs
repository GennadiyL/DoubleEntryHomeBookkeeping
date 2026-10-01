using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Templates;

/// <summary>
/// Supplies template properties and the complete ordered entry collection for saving.
/// The group and name select the template location and display identity.
/// Description is optional and independent of the name.
/// Entries may be empty or unbalanced; each present entry requires a valid account and amount.
/// Update preserves the template identity while replacing all existing entry rows.
/// Newly created entry positions follow the submitted list order.
/// Parent properties, entries and synchronization tracking commit as one aggregate.
/// Saving a template does not change transactions previously prepared from it.
/// </summary>
public record TemplateParam : INamedParam, IFavoriteParam, IElementParam
{
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
	public List<TemplateEntryParam> Entries { get; } = new();
}
