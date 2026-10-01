using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Templates;

public record TemplateParam : INamedParam, IFavoriteParam, IElementParam
{
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
	public List<TemplateEntryParam> Entries { get; } = new();
}
