using Business.Contracts.Params;
using Business.Contracts.Services;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class TemplateGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, ITemplateGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ITemplateGroupService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ITemplateGroupService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, ITemplateGroupService service)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, ITemplateGroupService service)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ITemplateGroupService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, ITemplateGroupService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}
}
