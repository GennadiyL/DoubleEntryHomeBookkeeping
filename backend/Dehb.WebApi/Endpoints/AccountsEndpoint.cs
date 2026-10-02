using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

internal static class AccountsEndpoint
{
	public static async Task<IResult> GetByIdHandler(Guid id, IAccountService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> GetDefaultNameHandler(Guid? correspondentId, Guid? categoryId, Guid? projectId,
		IAccountService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetDefaultName(correspondentId, categoryId, projectId, cancellationToken));

	public static async Task<IResult> AddHandler(AccountParam param, IAccountService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> UpdateHandler(UpdateAccountParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherGroupHandler(MoveToAnotherGroupParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.MoveToAnotherGroup(param.EntityId, param.ToGroupId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> CombineElementsHandler(CombineElementsParam param, IAccountService service, CancellationToken cancellationToken = default)
	{
		await service.CombineElements(param.ToElementId, param.FromElementId, cancellationToken);
		return Results.Ok();
	}
}
