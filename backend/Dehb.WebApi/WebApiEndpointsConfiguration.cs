using Dehb.WebApi.Endpoints;

namespace Dehb.WebApi;

/// <summary>
/// Defines the Web API endpoint composition entry point.
/// Maps the solution HTTP endpoint groups onto the application route builder.
/// The web host invokes it once after constructing the application.
/// It delegates audit-event route definitions to AuditEventsEndpoint.
/// It contains no endpoint business logic.
/// </summary>
public static class WebApiEndpointsConfiguration
{
	public static void AddEndpointsConfiguration(this WebApplication app)
	{
		app.MapPost("/account-groups/add", AccountGroupsEndpoint.AddHandler);
		app.MapPost("/category-groups/add", CategoryGroupsEndpoint.AddHandler);
		app.MapPost("/correspondent-groups/add", CorrespondentGroupsEndpoint.AddHandler);
		app.MapPost("/project-groups/add", ProjectGroupsEndpoint.AddHandler);
		app.MapPost("/template-groups/add", TemplateGroupsEndpoint.AddHandler);
		app.MapPost("/account-groups/{entityId:guid}/update", AccountGroupsEndpoint.UpdateHandler);
		app.MapPost("/category-groups/{entityId:guid}/update", CategoryGroupsEndpoint.UpdateHandler);
		app.MapPost("/correspondent-groups/{entityId:guid}/update", CorrespondentGroupsEndpoint.UpdateHandler);
		app.MapPost("/project-groups/{entityId:guid}/update", ProjectGroupsEndpoint.UpdateHandler);
		app.MapPost("/template-groups/{entityId:guid}/update", TemplateGroupsEndpoint.UpdateHandler);
		app.MapPost("/account-groups/{entityId:guid}/delete", AccountGroupsEndpoint.DeleteHandler);
		app.MapPost("/category-groups/{entityId:guid}/delete", CategoryGroupsEndpoint.DeleteHandler);
		app.MapPost("/correspondent-groups/{entityId:guid}/delete", CorrespondentGroupsEndpoint.DeleteHandler);
		app.MapPost("/project-groups/{entityId:guid}/delete", ProjectGroupsEndpoint.DeleteHandler);
		app.MapPost("/template-groups/{entityId:guid}/delete", TemplateGroupsEndpoint.DeleteHandler);
		app.MapPost("/account-groups/{entityId:guid}/set-order", AccountGroupsEndpoint.SetOrderHandler);
		app.MapPost("/category-groups/{entityId:guid}/set-order", CategoryGroupsEndpoint.SetOrderHandler);
		app.MapPost("/correspondent-groups/{entityId:guid}/set-order", CorrespondentGroupsEndpoint.SetOrderHandler);
		app.MapPost("/project-groups/{entityId:guid}/set-order", ProjectGroupsEndpoint.SetOrderHandler);
		app.MapPost("/template-groups/{entityId:guid}/set-order", TemplateGroupsEndpoint.SetOrderHandler);
	}
}
