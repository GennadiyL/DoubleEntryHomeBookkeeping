using Business.Contracts.Base.Params;

namespace Business.Contracts.Services.Accounts;

/// <summary>
/// Supplies account values for creation or editing.
/// GroupId selects an account group; CurrencyId selects the account currency.
/// The first successful save fixes the currency permanently.
/// Category, correspondent and project selections are optional references.
/// Name is trimmed and nonblank; duplicate account names are permitted.
/// Description is optional and independent of Name.
/// IsFavorite applies to this account; order and tracking are managed separately.
/// Services validate referenced entities and commit the complete action atomically.
/// </summary>
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
