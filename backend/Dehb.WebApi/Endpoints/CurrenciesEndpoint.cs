using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

/// <summary>
/// Exposes currency maintenance and selector reads over HTTP.
/// Delegates all validation and persistence to the currency service.
/// Binds currency metadata and the initial rate from request bodies.
/// Returns saved identities and detached currency projections.
/// Uses GET for catalog and editor reads.
/// Uses POST for currency mutations and ordering actions.
/// Forwards request cancellation to every service operation.
/// Centralized exception handling supplies error responses.
/// </summary>
internal static class CurrenciesEndpoint
{
	public static async Task<IResult> GetByIdHandler(Guid id, ICurrencyService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> GetAllCurrenciesHandler(ICurrencyService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetAllCurrencies(cancellationToken));

	public static async Task<IResult> GetAvailableCurrenciesHandler(ICurrencyService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetAvailableCurrencies(cancellationToken));

	public static async Task<IResult> AddHandler(AddCurrencyParam param, ICurrencyService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param.Code, param.InitialRate, cancellationToken));

	public static async Task<IResult> UpdateHandler(UpdateCurrencyParam param, ICurrencyService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICurrencyService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICurrencyService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICurrencyService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}
}
