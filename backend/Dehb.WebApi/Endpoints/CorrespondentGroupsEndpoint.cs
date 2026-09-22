using Business.Contracts.Params;
using Business.Contracts.Services;

namespace Dehb.WebApi.Endpoints;

internal static class CorrespondentGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, ICorrespondentGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(Guid entityId, int order, ICorrespondentGroupService service)
	{
		await service.SetOrder(entityId, order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(Guid entityId, bool isFavorite, ICorrespondentGroupService service)
	{
		await service.SetFavoriteStatus(entityId, isFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(Guid groupId, Guid toParentId, ICorrespondentGroupService service)
	{
		await service.MoveToAnotherParent(groupId, toParentId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(Guid toGroupId, Guid fromGroupId, ICorrespondentGroupService service)
	{
		await service.CombineGroups(toGroupId, fromGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(Guid entityId, ICorrespondentGroupService service)
	{
		await service.Delete(entityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(Guid entityId, GroupParam param, ICorrespondentGroupService service)
	{
		await service.Update(entityId, param);
		return Results.Ok();
	}
}
