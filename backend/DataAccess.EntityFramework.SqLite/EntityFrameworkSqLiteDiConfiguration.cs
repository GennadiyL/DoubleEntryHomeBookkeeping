using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.SqLite.Commands;
using Microsoft.Extensions.DependencyInjection;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Composes SQLite data access and optional Local aliases.
/// The factory reads database directories from IConfiguration.
/// All factory contexts open existing databases in read-write mode.
/// Local, Master and snapshot units use the same connection setup.
/// Each API dependency scope owns one caching factory instance.
/// Each cached unit receives one directly constructed common context.
/// Repository creation delegates use the current scoped service provider.
/// AddEntityFrameworkSqLiteModule registers the factory and SQLite commands.
/// AddLocalDataAccessSqLiteModule also binds AppDbContext and IAppUnitOfWork to Local.
/// Registration does not create, initialize or replace database files.
/// </summary>
public static class EntityFrameworkSqLiteDiConfiguration
{
	public static void AddEntityFrameworkSqLiteModule(this IServiceCollection services)
	{
		services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
		services.AddScoped<ICreateSnapshotCommand, CreateSnapshotCommand>();
		services.AddScoped<ICleanDataFilesCommand, CleanDataFilesCommand>();
		services.AddScoped<IPrepareCandidateCommand, PrepareCandidateCommand>();
		services.AddScoped<IValidateCandidateCommand, ValidateCandidateCommand>();
		services.AddScoped<IInstallCandidateCommand, InstallCandidateCommand>();
		services.AddScoped<ICumulativeAmountCommand, CumulativeAmountCommand>();
		services.AddEntityFrameworkModule();
	}

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
