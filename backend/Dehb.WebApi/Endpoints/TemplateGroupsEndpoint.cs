using Business.Contracts.Params;
using Business.Contracts.Services;

namespace Dehb.WebApi.Endpoints;

internal static class TemplateGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, ITemplateGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(Guid entityId, int order, ITemplateGroupService service)
	{
		await service.SetOrder(entityId, order);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(Guid entityId, ITemplateGroupService service)
	{
		await service.Delete(entityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(Guid entityId, GroupParam param, ITemplateGroupService service)
	{
		await service.Update(entityId, param);
		return Results.Ok();
	}
}
