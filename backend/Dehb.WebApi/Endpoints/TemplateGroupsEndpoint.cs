using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class TemplateGroupsEndpoint
{
	public static async Task<IResult> GetTreeHandler(ITemplateGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetTree(cancellationToken));

	public static async Task<IResult> GetAllGroupsHandler(ITemplateGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetAllGroups(cancellationToken));

	public static async Task<IResult> GetByIdHandler(Guid id, ITemplateGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> AddHandler(GroupParam param, ITemplateGroupService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherParentHandler(MoveToAnotherParentParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.MoveToAnotherParent(param.GroupId, param.ToParentId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> CombineGroupsHandler(CombineGroupsParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.CombineGroups(param.ToGroupId, param.FromGroupId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(UpdateGroupParam param, ITemplateGroupService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}
}
