using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class CorrespondentGroupsEndpoint
{
	public static async Task<IResult> GetTreeHandler(ICorrespondentGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTree(cancellationToken));

	public static async Task<IResult> GetAllGroupsHandler(ICorrespondentGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetAllGroups(cancellationToken));

	public static async Task<IResult> GetByIdHandler(Guid id, ICorrespondentGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> AddHandler(GroupParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, ICorrespondentGroupService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}
}
