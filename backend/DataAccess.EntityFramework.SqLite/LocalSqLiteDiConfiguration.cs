using DataAccess.Contracts;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Composes Local data access through the scoped unit-of-work factory.
/// Local and Candidate use separate instances of the common AppDbContext.
/// Existing business consumers resolve the factory-owned Local unit.
/// Context aliases share that unit's connection and transaction.
/// The factory owns disposal and explicit cache invalidation.
/// DataAccess:LocalDirectory identifies the Local database directory.
/// No derived Local context or keyed Local repository registration remains.
/// Registration does not create, initialize or replace database files.
/// </summary>
public static class LocalSqLiteDiConfiguration
{
	public static void AddLocalDataAccessSqLiteModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.TryAddSingleton(configuration);
		services.AddEntityFrameworkSqLiteModule();
		services.AddScoped<IAppUnitOfWork>(provider => provider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal());
		services.AddScoped<AppDbContext>(provider =>
			(AppDbContext)((IEntityFrameworkUnitOfWork)provider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal())
				.Database.GetService<ICurrentDbContext>().Context);
	}
}
