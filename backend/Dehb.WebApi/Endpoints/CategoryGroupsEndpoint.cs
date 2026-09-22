using Business.Contracts.Params;
using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class CategoryGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, ICategoryGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICategoryGroupService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICategoryGroupService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, ICategoryGroupService service)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, ICategoryGroupService service)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICategoryGroupService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, ICategoryGroupService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}
}
