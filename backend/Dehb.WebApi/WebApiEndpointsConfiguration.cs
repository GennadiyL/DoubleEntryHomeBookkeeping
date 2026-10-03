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
		app.MapGet("/transactions/get-by-id", TransactionsEndpoint.GetByIdHandler);
		app.MapGet("/transactions/get-transactions", TransactionsEndpoint.GetTransactionsHandler);
		app.MapGet("/transactions/duplicate-transaction", TransactionsEndpoint.DuplicateTransactionHandler);
		app.MapGet("/transactions/get-balances-for-all-accounts", TransactionsEndpoint.GetBalancesForAllAccountsHandler);
		app.MapGet("/transactions/get-balance-for-account", TransactionsEndpoint.GetBalanceForAccountHandler);
		app.MapPost("/transactions/add", TransactionsEndpoint.AddHandler);
		app.MapPost("/transactions/update", TransactionsEndpoint.UpdateHandler);
		app.MapPost("/transactions/delete", TransactionsEndpoint.DeleteHandler);
		app.MapPost("/transactions/delete-transactions", TransactionsEndpoint.DeleteTransactionsHandler);
		app.MapPost("/transactions/refresh-transactions", TransactionsEndpoint.RefreshTransactionsHandler);
		app.MapGet("/transactions/get-transactions-by-account", TransactionsEndpoint.GetTransactionsByAccountHandler);
		app.MapGet("/transactions/get-transactions-by-category", TransactionsEndpoint.GetTransactionsByCategoryHandler);
		app.MapGet("/transactions/get-transactions-by-correspondent", TransactionsEndpoint.GetTransactionsByCorrespondentHandler);
		app.MapGet("/transactions/get-transactions-by-project", TransactionsEndpoint.GetTransactionsByProjectHandler);
		app.MapPost("/transactions/delete-transactions-by-account", TransactionsEndpoint.DeleteTransactionsByAccountHandler);
		app.MapPost("/transactions/delete-transactions-by-category", TransactionsEndpoint.DeleteTransactionsByCategoryHandler);
		app.MapPost("/transactions/delete-transactions-by-correspondent", TransactionsEndpoint.DeleteTransactionsByCorrespondentHandler);
		app.MapPost("/transactions/delete-transactions-by-project", TransactionsEndpoint.DeleteTransactionsByProjectHandler);
		app.MapGet("/account-groups/get-accounts-tree", AccountGroupsEndpoint.GetAccountsTreeHandler);

		app.MapPost("/currency-rates/add-or-update", CurrencyRatesEndpoint.AddOrUpdateHandler);
		app.MapPost("/currency-rates/delete", CurrencyRatesEndpoint.DeleteHandler);
		app.MapGet("/currency-rates/get-rates", CurrencyRatesEndpoint.GetRatesHandler);
		app.MapGet("/currency-rates/get-rate", CurrencyRatesEndpoint.GetRateHandler);

		app.MapGet("/currencies/get-by-id", CurrenciesEndpoint.GetByIdHandler);
		app.MapGet("/currencies/get-all-currencies", CurrenciesEndpoint.GetAllCurrenciesHandler);
		app.MapGet("/currencies/get-available-currencies", CurrenciesEndpoint.GetAvailableCurrenciesHandler);
		app.MapPost("/currencies/add", CurrenciesEndpoint.AddHandler);
		app.MapPost("/currencies/update", CurrenciesEndpoint.UpdateHandler);
		app.MapPost("/currencies/delete", CurrenciesEndpoint.DeleteHandler);
		app.MapPost("/currencies/set-order", CurrenciesEndpoint.SetOrderHandler);
		app.MapPost("/currencies/set-favorite-status", CurrenciesEndpoint.SetFavoriteStatusHandler);

		app.MapGet("/templates/get-by-id", TemplatesEndpoint.GetByIdHandler);
		app.MapGet("/templates/apply-template", TemplatesEndpoint.ApplyTemplateHandler);
		app.MapGet("/templates/from-transaction", TemplatesEndpoint.FromTransactionHandler);
		app.MapPost("/templates/add", TemplatesEndpoint.AddHandler);
		app.MapPost("/templates/update", TemplatesEndpoint.UpdateHandler);
		app.MapPost("/templates/delete", TemplatesEndpoint.DeleteHandler);
		app.MapPost("/templates/set-order", TemplatesEndpoint.SetOrderHandler);
		app.MapPost("/templates/set-favorite-status", TemplatesEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/templates/move-to-another-group", TemplatesEndpoint.MoveToAnotherGroupHandler);
		app.MapPost("/templates/combine-elements", TemplatesEndpoint.CombineElementsHandler);

		app.MapGet("/accounts/get-by-id", AccountsEndpoint.GetByIdHandler);
		app.MapGet("/accounts/get-default-name", AccountsEndpoint.GetDefaultNameHandler);
		app.MapPost("/accounts/add", AccountsEndpoint.AddHandler);
		app.MapPost("/accounts/update", AccountsEndpoint.UpdateHandler);
		app.MapPost("/accounts/delete", AccountsEndpoint.DeleteHandler);
		app.MapPost("/accounts/set-order", AccountsEndpoint.SetOrderHandler);
		app.MapPost("/accounts/set-favorite-status", AccountsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/accounts/move-to-another-group", AccountsEndpoint.MoveToAnotherGroupHandler);
		app.MapPost("/accounts/combine-elements", AccountsEndpoint.CombineElementsHandler);

		app.MapGet("/account-groups/get-tree", AccountGroupsEndpoint.GetTreeHandler);
		app.MapGet("/category-groups/get-tree", CategoryGroupsEndpoint.GetTreeHandler);
		app.MapGet("/correspondent-groups/get-tree", CorrespondentGroupsEndpoint.GetTreeHandler);
		app.MapGet("/project-groups/get-tree", ProjectGroupsEndpoint.GetTreeHandler);
		app.MapGet("/template-groups/get-tree", TemplateGroupsEndpoint.GetTreeHandler);

		app.MapGet("/account-groups/get-all-groups", AccountGroupsEndpoint.GetAllGroupsHandler);
		app.MapGet("/category-groups/get-all-groups", CategoryGroupsEndpoint.GetAllGroupsHandler);
		app.MapGet("/correspondent-groups/get-all-groups", CorrespondentGroupsEndpoint.GetAllGroupsHandler);
		app.MapGet("/project-groups/get-all-groups", ProjectGroupsEndpoint.GetAllGroupsHandler);
		app.MapGet("/template-groups/get-all-groups", TemplateGroupsEndpoint.GetAllGroupsHandler);

		app.MapGet("/account-groups/get-by-id", AccountGroupsEndpoint.GetByIdHandler);
		app.MapGet("/category-groups/get-by-id", CategoryGroupsEndpoint.GetByIdHandler);
		app.MapGet("/correspondent-groups/get-by-id", CorrespondentGroupsEndpoint.GetByIdHandler);
		app.MapGet("/project-groups/get-by-id", ProjectGroupsEndpoint.GetByIdHandler);
		app.MapGet("/template-groups/get-by-id", TemplateGroupsEndpoint.GetByIdHandler);

		app.MapPost("/account-groups/add", AccountGroupsEndpoint.AddHandler);
		app.MapPost("/category-groups/add", CategoryGroupsEndpoint.AddHandler);
		app.MapPost("/correspondent-groups/add", CorrespondentGroupsEndpoint.AddHandler);
		app.MapPost("/project-groups/add", ProjectGroupsEndpoint.AddHandler);
		app.MapPost("/template-groups/add", TemplateGroupsEndpoint.AddHandler);
		app.MapGet("/categories/get-by-id", CategoriesEndpoint.GetByIdHandler);
		app.MapGet("/correspondents/get-by-id", CorrespondentsEndpoint.GetByIdHandler);
		app.MapGet("/projects/get-by-id", ProjectsEndpoint.GetByIdHandler);

		app.MapPost("/categories/add", CategoriesEndpoint.AddHandler);
		app.MapPost("/correspondents/add", CorrespondentsEndpoint.AddHandler);
		app.MapPost("/projects/add", ProjectsEndpoint.AddHandler);

		app.MapPost("/account-groups/update", AccountGroupsEndpoint.UpdateHandler);
		app.MapPost("/category-groups/update", CategoryGroupsEndpoint.UpdateHandler);
		app.MapPost("/correspondent-groups/update", CorrespondentGroupsEndpoint.UpdateHandler);
		app.MapPost("/project-groups/update", ProjectGroupsEndpoint.UpdateHandler);
		app.MapPost("/template-groups/update", TemplateGroupsEndpoint.UpdateHandler);
		app.MapPost("/categories/update", CategoriesEndpoint.UpdateHandler);
		app.MapPost("/correspondents/update", CorrespondentsEndpoint.UpdateHandler);
		app.MapPost("/projects/update", ProjectsEndpoint.UpdateHandler);

		app.MapPost("/account-groups/delete", AccountGroupsEndpoint.DeleteHandler);
		app.MapPost("/category-groups/delete", CategoryGroupsEndpoint.DeleteHandler);
		app.MapPost("/correspondent-groups/delete", CorrespondentGroupsEndpoint.DeleteHandler);
		app.MapPost("/project-groups/delete", ProjectGroupsEndpoint.DeleteHandler);
		app.MapPost("/template-groups/delete", TemplateGroupsEndpoint.DeleteHandler);
		app.MapPost("/categories/delete", CategoriesEndpoint.DeleteHandler);
		app.MapPost("/correspondents/delete", CorrespondentsEndpoint.DeleteHandler);
		app.MapPost("/projects/delete", ProjectsEndpoint.DeleteHandler);

		app.MapPost("/account-groups/set-order", AccountGroupsEndpoint.SetOrderHandler);
		app.MapPost("/category-groups/set-order", CategoryGroupsEndpoint.SetOrderHandler);
		app.MapPost("/correspondent-groups/set-order", CorrespondentGroupsEndpoint.SetOrderHandler);
		app.MapPost("/project-groups/set-order", ProjectGroupsEndpoint.SetOrderHandler);
		app.MapPost("/template-groups/set-order", TemplateGroupsEndpoint.SetOrderHandler);
		app.MapPost("/categories/set-order", CategoriesEndpoint.SetOrderHandler);
		app.MapPost("/correspondents/set-order", CorrespondentsEndpoint.SetOrderHandler);
		app.MapPost("/projects/set-order", ProjectsEndpoint.SetOrderHandler);

		app.MapPost("/account-groups/set-favorite-status", AccountGroupsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/category-groups/set-favorite-status", CategoryGroupsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/correspondent-groups/set-favorite-status", CorrespondentGroupsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/project-groups/set-favorite-status", ProjectGroupsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/template-groups/set-favorite-status", TemplateGroupsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/categories/set-favorite-status", CategoriesEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/correspondents/set-favorite-status", CorrespondentsEndpoint.SetFavoriteStatusHandler);
		app.MapPost("/projects/set-favorite-status", ProjectsEndpoint.SetFavoriteStatusHandler);

		app.MapPost("/account-groups/move-to-another-parent", AccountGroupsEndpoint.MoveToAnotherParentHandler);
		app.MapPost("/category-groups/move-to-another-parent", CategoryGroupsEndpoint.MoveToAnotherParentHandler);
		app.MapPost("/correspondent-groups/move-to-another-parent", CorrespondentGroupsEndpoint.MoveToAnotherParentHandler);
		app.MapPost("/project-groups/move-to-another-parent", ProjectGroupsEndpoint.MoveToAnotherParentHandler);
		app.MapPost("/template-groups/move-to-another-parent", TemplateGroupsEndpoint.MoveToAnotherParentHandler);
		app.MapPost("/categories/move-to-another-group", CategoriesEndpoint.MoveToAnotherGroupHandler);
		app.MapPost("/correspondents/move-to-another-group", CorrespondentsEndpoint.MoveToAnotherGroupHandler);
		app.MapPost("/projects/move-to-another-group", ProjectsEndpoint.MoveToAnotherGroupHandler);

		app.MapPost("/account-groups/combine-groups", AccountGroupsEndpoint.CombineGroupsHandler);
		app.MapPost("/category-groups/combine-groups", CategoryGroupsEndpoint.CombineGroupsHandler);
		app.MapPost("/correspondent-groups/combine-groups", CorrespondentGroupsEndpoint.CombineGroupsHandler);
		app.MapPost("/project-groups/combine-groups", ProjectGroupsEndpoint.CombineGroupsHandler);
		app.MapPost("/template-groups/combine-groups", TemplateGroupsEndpoint.CombineGroupsHandler);
		app.MapPost("/categories/combine-elements", CategoriesEndpoint.CombineElementsHandler);
		app.MapPost("/correspondents/combine-elements", CorrespondentsEndpoint.CombineElementsHandler);
		app.MapPost("/projects/combine-elements", ProjectsEndpoint.CombineElementsHandler);
	}
}
