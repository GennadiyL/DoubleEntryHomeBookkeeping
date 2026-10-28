using DataAccess.Contracts;
using DataAccess.Core.Behaviors;
using DataAccess.Contracts.Repositories;
using DataAccess.EntityFramework.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccess.EntityFramework;

/// <summary>
/// Registers shared EF implementations separately for Local and Master.
/// Each role has its own keyed scoped repository registrations.
/// Repository factories receive the context registered under that role's key.
/// The stateless mapper is shared by both role registrations.
/// Existing unkeyed repository consumers resolve Local.
/// Master registrations never replace these Local aliases.
/// Provider modules register concrete units of work, contexts and connection options.
/// No service provider or nested container is built during registration.
/// </summary>
public static class EntityFrameworkDiConfiguration
{
	public static void AddLocalDataAccessEntityFrameworkModule(this IServiceCollection services)
	{
		AddRepositories(services, typeof(ILocalAppUnitOfWork));
	}

	public static void AddMasterDataAccessEntityFrameworkModule(this IServiceCollection services)
	{
		AddRepositories(services, typeof(IMasterAppUnitOfWork));
	}

	public static void AddFactoryDataAccessEntityFrameworkModule(this IServiceCollection services)
	{
		AddRepositories(services, typeof(IUnitOfWorkFactory));
	}

	private static void AddRepositories(IServiceCollection services, Type databaseKey)
	{
		services.TryAddScoped<IMapper, AppMapper>();
		AddRepository<IAccountRepository>(services, databaseKey, (context, mapper) => new AccountRepository(context, mapper));
		AddRepository<IAccountGroupRepository>(services, databaseKey, (context, mapper) => new AccountGroupRepository(context, mapper));
		AddRepository<ICategoryRepository>(services, databaseKey, (context, mapper) => new CategoryRepository(context, mapper));
		AddRepository<ICategoryGroupRepository>(services, databaseKey, (context, mapper) => new CategoryGroupRepository(context, mapper));
		AddRepository<ICorrespondentRepository>(services, databaseKey, (context, mapper) => new CorrespondentRepository(context, mapper));
		AddRepository<ICorrespondentGroupRepository>(services, databaseKey, (context, mapper) => new CorrespondentGroupRepository(context, mapper));
		AddRepository<ICurrencyRepository>(services, databaseKey, (context, mapper) => new CurrencyRepository(context, mapper));
		AddRepository<ICurrencyRateRepository>(services, databaseKey, (context, mapper) => new CurrencyRateRepository(context, mapper));
		AddRepository<IProjectRepository>(services, databaseKey, (context, mapper) => new ProjectRepository(context, mapper));
		AddRepository<IReportRepository>(services, databaseKey, (context, mapper) => new ReportRepository(context, mapper));
		AddRepository<IReportGroupRepository>(services, databaseKey, (context, mapper) => new ReportGroupRepository(context, mapper));
		AddRepository<IProjectGroupRepository>(services, databaseKey, (context, mapper) => new ProjectGroupRepository(context, mapper));
		AddRepository<ITemplateRepository>(services, databaseKey, (context, mapper) => new TemplateRepository(context, mapper));
		AddRepository<ITemplateEntryRepository>(services, databaseKey, (context, mapper) => new TemplateEntryRepository(context, mapper));
		AddRepository<ITemplateGroupRepository>(services, databaseKey, (context, mapper) => new TemplateGroupRepository(context, mapper));
		AddRepository<ITransactionRepository>(services, databaseKey, (context, mapper) => new TransactionRepository(context, mapper));
		AddRepository<ITransactionEntryRepository>(services, databaseKey, (context, mapper) => new TransactionEntryRepository(context, mapper));
		AddRepository<ISystemConfigRepository>(services, databaseKey, (context, mapper) => new SystemConfigRepository(context, mapper));
		AddRepository<ILocalJsonRepository>(services, databaseKey, (context, mapper) => new LocalJsonRepository(context, mapper));
		AddRepository<ILocalConfigRepository>(services, databaseKey, (context, mapper) => new LocalConfigRepository(context, mapper));
		AddRepository<ILocalRegistrationRepository>(services, databaseKey, (context, mapper) => new LocalRegistrationRepository(context, mapper));
		AddRepository<ILocalSyncBatchRepository>(services, databaseKey, (context, mapper) => new LocalSyncBatchRepository(context, mapper));
	}

	private static void AddRepository<TRepository>(
		IServiceCollection services,
		Type databaseKey,
		Func<AppDbContext, IMapper, TRepository> factory)
		where TRepository : class
	{
		if (databaseKey == typeof(IUnitOfWorkFactory))
		{
			services.AddScoped<Func<AppDbContext, TRepository>>(provider =>
				context => factory(context, provider.GetRequiredService<IMapper>()));
			return;
		}
		services.AddKeyedScoped<TRepository>(databaseKey, (provider, _) =>
			factory(provider.GetRequiredKeyedService<AppDbContext>(databaseKey), provider.GetRequiredService<IMapper>()));
		if (databaseKey == typeof(ILocalAppUnitOfWork))
		{
			services.AddScoped<TRepository>(provider => provider.GetRequiredKeyedService<TRepository>(databaseKey));
		}
	}
}
