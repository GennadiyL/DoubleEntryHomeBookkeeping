using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DataAccess.Contracts;
using DataAccess.Core.Behaviors;
using DataAccess.Contracts.Repositories;
using DataAccess.EntityFramework.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccess.EntityFramework;

/// <summary>
/// Registers shared EF repository factories for all database roles.
/// Each unit creates repositories bound to its own AppDbContext.
/// Repository delegates receive the context selected by the unit of work.
/// The stateless mapper is shared by both role registrations.
/// Existing unkeyed repository consumers resolve Local.
/// Master registrations never replace these Local aliases.
/// Provider modules register concrete units of work, contexts and connection options.
/// No service provider or nested container is built during registration.
/// </summary>
public static class EntityFrameworkDiConfiguration
{
	public static void AddEntityFrameworkModule(this IServiceCollection services)
	{
		AddRepositories(services);
	}

	private static void AddRepositories(IServiceCollection services)
	{
		services.TryAddScoped<IMapper, AppMapper>();
		AddRepository<IAccountRepository>(services, (context, mapper) => new AccountRepository(context, mapper));
		AddRepository<IAccountGroupRepository>(services, (context, mapper) => new AccountGroupRepository(context, mapper));
		AddRepository<ICategoryRepository>(services, (context, mapper) => new CategoryRepository(context, mapper));
		AddRepository<ICategoryGroupRepository>(services, (context, mapper) => new CategoryGroupRepository(context, mapper));
		AddRepository<ICorrespondentRepository>(services, (context, mapper) => new CorrespondentRepository(context, mapper));
		AddRepository<ICorrespondentGroupRepository>(services, (context, mapper) => new CorrespondentGroupRepository(context, mapper));
		AddRepository<ICurrencyRepository>(services, (context, mapper) => new CurrencyRepository(context, mapper));
		AddRepository<ICurrencyRateRepository>(services, (context, mapper) => new CurrencyRateRepository(context, mapper));
		AddRepository<IProjectRepository>(services, (context, mapper) => new ProjectRepository(context, mapper));
		AddRepository<IReportRepository>(services, (context, mapper) => new ReportRepository(context, mapper));
		AddRepository<IReportGroupRepository>(services, (context, mapper) => new ReportGroupRepository(context, mapper));
		AddRepository<IProjectGroupRepository>(services, (context, mapper) => new ProjectGroupRepository(context, mapper));
		AddRepository<ITemplateRepository>(services, (context, mapper) => new TemplateRepository(context, mapper));
		AddRepository<ITemplateEntryRepository>(services, (context, mapper) => new TemplateEntryRepository(context, mapper));
		AddRepository<ITemplateGroupRepository>(services, (context, mapper) => new TemplateGroupRepository(context, mapper));
		AddRepository<ITransactionRepository>(services, (context, mapper) => new TransactionRepository(context, mapper));
		AddRepository<ITransactionEntryRepository>(services, (context, mapper) => new TransactionEntryRepository(context, mapper));
		AddRepository<ISystemConfigRepository>(services, (context, mapper) => new SystemConfigRepository(context, mapper));
		AddRepository<ILocalJsonRepository>(services, (context, mapper) => new LocalJsonRepository(context, mapper));
		AddRepository<ILocalConfigRepository>(services, (context, mapper) => new LocalConfigRepository(context, mapper));
		AddRepository<ILocalRegistrationRepository>(services, (context, mapper) => new LocalRegistrationRepository(context, mapper));
		AddRepository<ILocalSyncBatchRepository>(services, (context, mapper) => new LocalSyncBatchRepository(context, mapper));
	}

	private static void AddRepository<TRepository>(
		IServiceCollection services,
		Func<AppDbContext, IMapper, TRepository> factory)
		where TRepository : class
	{
		services.AddScoped<Func<AppDbContext, TRepository>>(provider =>
			context => factory(context, provider.GetRequiredService<IMapper>()));
		services.AddScoped<TRepository>(provider =>
			factory(
				(AppDbContext)((IEntityFrameworkUnitOfWork)provider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal())
					.Database.GetService<ICurrentDbContext>().Context,
				provider.GetRequiredService<IMapper>()));
	}
}
