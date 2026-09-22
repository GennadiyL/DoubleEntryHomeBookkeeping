using Business.Contracts.Params;
using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class CorrespondentsEndpoint
{
	public static async Task<IResult> MoveToAnotherGroupHandler(MoveToAnotherGroupParam param, ICorrespondentService service)
	{
		await service.MoveToAnotherGroup(param.EntityId, param.ToGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineElementsHandler(CombineElementsParam param, ICorrespondentService service)
	{
		await service.CombineElements(param.ToElementId, param.FromElementId);
		return Results.Ok();
	}

	public static async Task<IResult> AddHandler(ElementParam param, ICorrespondentService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> UpdateHandler(UpdateElementParam param, ICorrespondentService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICorrespondentService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICorrespondentService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICorrespondentService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}
}
