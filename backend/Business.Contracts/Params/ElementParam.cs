using Business.Contracts.Params.Interfaces;

namespace Business.Contracts.Params;

public class ElementParam : INamedParam, IFavoriteParam, IElementParam
{
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}
