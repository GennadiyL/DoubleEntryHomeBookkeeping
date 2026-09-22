using Business.Contracts.Params;
using Business.Contracts.Services;

namespace Dehb.WebApi.Endpoints;

internal static class CategoryGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, ICategoryGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(Guid entityId, int order, ICategoryGroupService service)
	{
		await service.SetOrder(entityId, order);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(Guid entityId, ICategoryGroupService service)
	{
		await service.Delete(entityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(Guid entityId, GroupParam param, ICategoryGroupService service)
	{
		await service.Update(entityId, param);
		return Results.Ok();
	}
}
