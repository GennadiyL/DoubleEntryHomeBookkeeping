using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.SqLite.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Composes the Master SQLite data-access implementation.
/// Requires the DehbMasterSqLite connection string from host configuration.
/// Registers typed context options and the role-specific unit of work.
/// Keyed repository factories resolve this context within the current scope.
/// Cumulative commands use the same context and transaction boundary.
/// Local and Master modules can coexist in one dependency container.
/// Shared persistence mappings are inherited from SqLiteAppDbContext.
/// Registration does not open, initialize or replace a database.
/// </summary>
public static class MasterSqLiteDiConfiguration
{
	public static void AddMasterDataAccessSqLiteModule(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		string connectionString = configuration.GetConnectionString("DehbMasterSqLite")
			?? throw new InvalidOperationException("Connection string 'DehbMasterSqLite' is not configured.");
		AddMasterDataAccessSqLite(services, connectionString);
	}

	private static void AddMasterDataAccessSqLite(IServiceCollection services, string connectionString)
	{
		services.AddMasterDataAccessEntityFrameworkModule();
		services.AddDbContext<MasterAppDbContext>(options => options.UseSqlite(connectionString));
		services.AddScoped<IMasterAppUnitOfWork, MasterAppUnitOfWork>();
		services.AddKeyedScoped<AppDbContext>(typeof(IMasterAppUnitOfWork),
			(provider, _) => provider.GetRequiredService<MasterAppDbContext>());
		services.AddKeyedScoped<ICumulativeAmountCommand>(typeof(IMasterAppUnitOfWork),
			(provider, _) => new CumulativeAmountCommand(provider.GetRequiredService<MasterAppDbContext>()));
	}
}
