using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Accounts;

public record AccountParam : INamedParam, IFavoriteParam, IElementParam
{
	public Guid CurrencyId { get; set; }
	public Guid? CategoryId { get; set; }
	public Guid? CorrespondentId { get; set; }
	public Guid? ProjectId { get; set; }
	public Guid GroupId { get; set; }
	public bool IsFavorite { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}
