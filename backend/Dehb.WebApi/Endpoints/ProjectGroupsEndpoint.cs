using Business.Contracts.Params;
using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class ProjectGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, IProjectGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, IProjectGroupService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, IProjectGroupService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, IProjectGroupService service)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, IProjectGroupService service)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, IProjectGroupService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, IProjectGroupService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}
}
