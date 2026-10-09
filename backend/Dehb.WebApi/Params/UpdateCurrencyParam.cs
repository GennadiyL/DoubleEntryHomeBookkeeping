using Business.Contracts.Services.Currencies;

namespace Dehb.WebApi.Params;

/// <summary>
/// Binds an existing currency identity and its editor metadata.
/// EntityId identifies the currency to update.
/// ISO code, symbol and English name are not mutation inputs.
/// The inherited Name contains the requested short display value (1 to 6 characters).
/// Initial rates and other rate history are outside this request.
/// Favorite status and ordering use separate catalog actions.
/// Validation and synchronization belong to the currency service.
/// The endpoint passes the identity separately from the metadata.
/// </summary>
public record UpdateCurrencyParam : CurrencyParam
{
	public Guid EntityId { get; set; }
}
