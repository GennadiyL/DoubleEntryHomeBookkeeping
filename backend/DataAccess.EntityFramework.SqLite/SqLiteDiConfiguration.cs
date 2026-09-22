using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Defines the SqLite data-access composition entry point.
/// Configures AppDbContext for SqLite using the application connection settings.
/// Infrastructure hosts call the module during startup when SqLite is selected.
/// It reuses the provider-independent Entity Framework registrations.
/// It contains no persistence queries or business behavior.
/// </summary>
public static class SqLiteDiConfiguration
{
	public static void AddDataAccessSqLiteModule(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		string connectionString = configuration.GetConnectionString("AppDb")
			?? throw new InvalidOperationException("Connection string 'AppDb' is not configured.");

		services.AddDataAccessEntityFrameworkModule();
		services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
	}
}
