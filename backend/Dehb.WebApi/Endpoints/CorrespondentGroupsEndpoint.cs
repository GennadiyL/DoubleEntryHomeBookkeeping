using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class CorrespondentGroupsEndpoint
{
	public static async Task<IResult> GetTreeHandler(ICorrespondentGroupService service) =>
		Results.Ok(await service.GetTree());

	public static async Task<IResult> GetAllGroupsHandler(ICorrespondentGroupService service) =>
		Results.Ok(await service.GetAllGroups());

	public static async Task<IResult> GetByIdHandler(Guid id, ICorrespondentGroupService service) =>
		Results.Ok(await service.GetById(id));

	public static async Task<IResult> AddHandler(GroupParam param, ICorrespondentGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICorrespondentGroupService service)
	{
		await service.SetOrder(param.EntityId, param.Order);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICorrespondentGroupService service)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, ICorrespondentGroupService service)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, ICorrespondentGroupService service)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICorrespondentGroupService service)
	{
		await service.Delete(param.EntityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, ICorrespondentGroupService service)
	{
		await service.Update(param.EntityId, param);
		return Results.Ok();
	}
}
