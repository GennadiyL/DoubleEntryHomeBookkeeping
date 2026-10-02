namespace Dehb.WebApi.Params;

/// <summary>
/// Selects ordinary currency rates for a range deletion request.
/// CurrencyId identifies the saved currency whose history is edited.
/// FromDate and ToDate are inclusive calendar-date boundaries.
/// The service rejects reversed ranges before changing data.
/// The initial fallback rate remains protected even inside the range.
/// Previously deleted rates are not changed again.
/// Deletion follows the shared soft-delete lifecycle.
/// All selected changes are committed together by the service.
/// </summary>
public record DeleteCurrencyRatesParam
{
	public Guid CurrencyId { get; set; }
	public DateOnly FromDate { get; set; }
	public DateOnly ToDate { get; set; }
}
