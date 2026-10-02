using Business.Contracts.Services.Currencies;

namespace Dehb.WebApi.Params;

/// <summary>
/// Binds currency creation metadata and its initial rate from one request body.
/// Inherits the ISO code, display name and symbol.
/// InitialRate supplies the protected fallback rate value.
/// The service validates precision and assigns the initial date.
/// No currency or rate identity is supplied by the caller.
/// Tracking is initialized by the currency service.
/// Both rows are committed together by the business operation.
/// This request does not change existing currency rates.
/// </summary>
public record AddCurrencyParam : CurrencyParam
{
	public decimal InitialRate { get; set; }
}
