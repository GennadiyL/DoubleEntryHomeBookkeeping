using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.SqLite.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Registers the additive SQLite unit-of-work factory API.
/// The factory reads database directories from IConfiguration.
/// All factory contexts open existing databases in read-write mode.
/// Local, Master and snapshot units use the same connection setup.
/// Each API dependency scope owns one caching factory instance.
/// Each cached unit receives one directly constructed common context.
/// Repository creation delegates use the current scoped service provider.
/// Existing context and unit-of-work registrations remain unchanged.
/// </summary>
public static class EntityFrameworkSqLiteDiConfiguration
{
	public static void AddEntityFrameworkSqLiteModule(this IServiceCollection services)
	{
		services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
		services.AddScoped<ICreateSnapshotCommand, CreateSnapshotCommand>();
		services.AddScoped<ICumulativeAmountCommand, CumulativeAmountCommand>();
		services.AddDataAccessEntityFrameworkModule();
	}
}
