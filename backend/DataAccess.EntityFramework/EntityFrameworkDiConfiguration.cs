using DataAccess.Contracts;
using DataAccess.Core.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

/// <summary>
/// Defines the Entity Framework composition entry point.
/// Registers the mapper, unit of work, and application repositories.
/// Provider-specific modules call this configuration during startup.
/// It exposes composition publicly while concrete persistence implementations remain internal.
/// It does not select a database provider or connection string.
/// </summary>
public static class EntityFrameworkDiConfiguration
{
	public static void AddDataAccessEntityFrameworkModule(this IServiceCollection services)
	{
		services.AddScoped<IMapper, AppMapper>();
		services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
	}
}
