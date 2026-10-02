using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class CategoriesEndpoint
{
	public static async Task<IResult> GetByIdHandler(Guid id, ICategoryService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> MoveToAnotherGroupHandler(MoveToAnotherGroupParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.MoveToAnotherGroup(param.EntityId, param.ToGroupId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> CombineElementsHandler(CombineElementsParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.CombineElements(param.ToElementId, param.FromElementId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> AddHandler(ElementParam param, ICategoryService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> UpdateHandler(UpdateElementParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ICategoryService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}
}
