namespace Dehb.WebApi.Params;

/// <summary>
/// Binds currency creation metadata and its initial rate from one request body.
/// Accepts an ISO code; catalog metadata and the initial Name are assigned by the service.
/// InitialRate supplies the protected fallback rate value.
/// The service validates precision and assigns the initial date.
/// No currency or rate identity is supplied by the caller.
/// Tracking is initialized by the currency service.
/// Both rows are committed together by the business operation.
/// This request does not change existing currency rates.
/// </summary>
public record AddCurrencyParam
{
	public required string Code { get; set; }

	public decimal InitialRate { get; set; }
}
