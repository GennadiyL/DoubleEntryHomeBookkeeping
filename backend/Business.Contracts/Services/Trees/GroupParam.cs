using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Trees;

public record GroupParam : INamedParam, IFavoriteParam, IGroupParam
{
	public Guid ParentId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}
