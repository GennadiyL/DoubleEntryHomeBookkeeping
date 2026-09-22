using Business.Contracts.Services;
using Business.Impl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Impl;

/// <summary>
/// Defines the business-layer composition entry point.
/// Registers business services and operations with the dependency injection container.
/// Infrastructure hosts call AddBusinessModule during application startup.
/// It exposes composition publicly while keeping concrete business implementations internal.
/// It contains no business workflow logic.
/// </summary>
public static class BusinessDiConfiguration
{
	public static void AddBusinessModule(this IServiceCollection services)
	{
		services.AddScoped<IAccountService, AccountService>();
		services.AddScoped<IAccountGroupService, AccountGroupService>();
		services.AddScoped<ICategoryGroupService, CategoryGroupService>();
		services.AddScoped<ICorrespondentGroupService, CorrespondentGroupService>();
		services.AddScoped<IProjectGroupService, ProjectGroupService>();
		services.AddScoped<ITemplateGroupService, TemplateGroupService>();
		services.AddScoped<ICategoryService, CategoryService>();
		services.AddScoped<ICorrespondentService, CorrespondentService>();
		services.AddScoped<IProjectService, ProjectService>();
	}
}
