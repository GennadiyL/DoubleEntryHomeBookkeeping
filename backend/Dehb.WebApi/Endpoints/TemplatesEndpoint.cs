using Business.Contracts.Services;
using Business.Contracts.Services.Templates;
using Dehb.WebApi.Params;

namespace Dehb.WebApi.Endpoints;

/// <summary>
/// Exposes template operations through thin HTTP endpoint handlers.
/// Binds editor inputs and forwards them to the template service.
/// Returns saved identities or detached editor projections.
/// Applies cancellation from the current HTTP request.
/// Template and transaction previews do not persist changes.
/// Catalog commands share the template service validation rules.
/// Aggregate entry replacement remains inside the business layer.
/// Exceptions are handled by the centralized Web API handler.
/// </summary>
internal static class TemplatesEndpoint
{
	public static async Task<IResult> GetByIdHandler(Guid id, ITemplateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.GetById(id, cancellationToken));

	public static async Task<IResult> ApplyTemplateHandler(Guid templateId, ITemplateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.ApplyTemplate(templateId, cancellationToken));

	public static async Task<IResult> FromTransactionHandler(Guid transactionId, ITemplateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.FromTransaction(transactionId, cancellationToken));

	public static async Task<IResult> AddHandler(TemplateParam param, ITemplateService service, CancellationToken cancellationToken = default) =>
		Results.Ok(await service.Add(param, cancellationToken));

	public static async Task<IResult> UpdateHandler(UpdateTemplateParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.Update(param.EntityId, param, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> DeleteHandler(DeleteParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.Delete(param.EntityId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetOrderHandler(SetOrderParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.SetOrder(param.EntityId, param.Order, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> SetFavoriteStatusHandler(SetFavoriteStatusParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.SetFavoriteStatus(param.EntityId, param.IsFavorite, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> MoveToAnotherGroupHandler(MoveToAnotherGroupParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.MoveToAnotherGroup(param.EntityId, param.ToGroupId, cancellationToken);
		return Results.Ok();
	}

	public static async Task<IResult> CombineElementsHandler(CombineElementsParam param, ITemplateService service, CancellationToken cancellationToken = default)
	{
		await service.CombineElements(param.ToElementId, param.FromElementId, cancellationToken);
		return Results.Ok();
	}
}
