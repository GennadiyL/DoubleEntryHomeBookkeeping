using Business.Contracts.Services.Currencies;

namespace Dehb.WebApi.Params;

/// <summary>
/// Binds an existing currency identity and its editor metadata.
/// EntityId identifies the currency to update.
/// The inherited ISO code must match the stored immutable code.
/// Name and symbol contain the requested display values.
/// Initial rates and other rate history are outside this request.
/// Favorite status and ordering use separate catalog actions.
/// Validation and synchronization belong to the currency service.
/// The endpoint passes the identity separately from the metadata.
/// </summary>
public record UpdateCurrencyParam : CurrencyParam
{
	public Guid EntityId { get; set; }
}
