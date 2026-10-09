namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Supplies the editable short name of a saved currency.
/// Name contains one to six characters after trimming.
/// Code, Symbol and EnglishName remain catalog-owned values.
/// Only Name is editable through this request.
/// Creation accepts an ISO code and initial rate separately.
/// Favorite status and catalog ordering use their separate operations.
/// Identity and synchronization metadata are assigned by the service.
/// Currency creation and its initial rate commit together.
/// </summary>
public record CurrencyParam
{
	public required string Name { get; set; }
}
