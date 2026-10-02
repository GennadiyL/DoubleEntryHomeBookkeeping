using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

/// <summary>
/// Exposes currency-rate maintenance and editor lookups over HTTP.
/// Binds currency, account and date selectors to the service contract.
/// Uses POST for upserts and inclusive range deletion.
/// Uses GET for rate history and applicable-rate lookup.
/// Returns detached projections, saved identities or the selected rate.
/// Forwards cancellation from the HTTP request.
/// Business rules and commits remain within the rate service.
/// The centralized exception handler supplies failure responses.
/// </summary>
internal static class CurrencyRatesEndpoint
{
	public static async Task<IResult> AddOrUpdateHandler(CurrencyRateParam param, ICurrencyRateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.AddOrUpdate(param, cancellationToken));

	public static async Task<IResult> DeleteHandler(DeleteCurrencyRatesParam param, ICurrencyRateService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.CurrencyId, param.FromDate, param.ToDate, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> GetRatesHandler(Guid currencyId, ICurrencyRateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetRates(currencyId, cancellationToken));

	public static async Task<IResult> GetRateHandler(Guid accountId, DateOnly date, ICurrencyRateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetRate(accountId, date, cancellationToken));
}
