using Business.Contracts.Params;
using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class AccountsEndpoint
{
	public static async Task<IResult> AddHandler(AccountParam param, IAccountService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> UpdateHandler(UpdateAccountParam param, IAccountService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, IAccountService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, IAccountService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, IAccountService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherGroupHandler(MoveToAnotherGroupParam param, IAccountService service)
	{
		await service.MoveToAnotherGroup(param.EntityId, param.ToGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineElementsHandler(CombineElementsParam param, IAccountService service)
	{
		await service.CombineElements(param.ToElementId, param.FromElementId);
		return Results.Ok();
	}
}
