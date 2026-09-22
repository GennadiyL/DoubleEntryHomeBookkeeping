using Business.Contracts.Params;
using Business.Contracts.Services;

namespace Dehb.WebApi.Endpoints;

internal static class AccountGroupsEndpoint
{
	public static async Task<IResult> AddHandler(GroupParam param, IAccountGroupService service) =>
		Results.Ok(await service.Add(param));

	public static async Task<IResult> SetOrderHandler(Guid entityId, int order, IAccountGroupService service)
	{
		await service.SetOrder(entityId, order);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(Guid entityId, IAccountGroupService service)
	{
		await service.Delete(entityId);
		return Results.Ok();
	}

	public static async Task<IResult> UpdateHandler(Guid entityId, GroupParam param, IAccountGroupService service)
	{
		await service.Update(entityId, param);
		return Results.Ok();
	}
}
