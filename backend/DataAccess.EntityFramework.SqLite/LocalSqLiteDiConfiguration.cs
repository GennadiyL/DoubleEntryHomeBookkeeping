using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.SqLite.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Composes the Local SQLite data-access implementation.
/// Reads the DehbSqLite connection string from host configuration.
/// Registers typed context options and the role-specific unit of work.
/// Keyed repository factories resolve this context within the current scope.
/// Cumulative commands use the same context and transaction boundary.
/// Local and Master modules can coexist in one dependency container.
/// Shared persistence mappings are inherited from SqLiteAppDbContext.
/// Registration does not open, initialize or replace a database.
/// </summary>
public static class LocalSqLiteDiConfiguration
{
	public static void AddLocalDataAccessSqLiteModule(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		string connectionString = configuration.GetConnectionString("DehbSqLite")
			?? throw new InvalidOperationException("Connection string 'DehbSqLite' is not configured.");
		services.AddLocalDataAccessEntityFrameworkModule();
		services.AddDbContext<LocalAppDbContext>(options => options.UseSqlite(connectionString));
		services.AddScoped<ILocalAppUnitOfWork, LocalAppUnitOfWork>();
		services.AddScoped<IAppUnitOfWork>(provider => provider.GetRequiredService<ILocalAppUnitOfWork>());
		services.AddKeyedScoped<AppDbContext>(typeof(ILocalAppUnitOfWork),
			(provider, _) => provider.GetRequiredService<LocalAppDbContext>());
		services.AddKeyedScoped<ICumulativeAmountCommand>(typeof(ILocalAppUnitOfWork),
			(provider, _) => new CumulativeAmountCommand(provider.GetRequiredService<LocalAppDbContext>()));

		services.AddScoped<AppDbContext>(provider => provider.GetRequiredService<LocalAppDbContext>());
		services.AddScoped<ICumulativeAmountCommand>(provider =>
			provider.GetRequiredKeyedService<ICumulativeAmountCommand>(typeof(ILocalAppUnitOfWork)));
	}
}
