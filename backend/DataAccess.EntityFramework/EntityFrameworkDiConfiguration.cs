using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

public static class EntityFrameworkDiConfiguration
{
	public static void AddDataAccessEntityFrameworkModule(this IServiceCollection services)
	{
		services.AddScoped<IMapper, AppMapper>();
		services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
		
		services.AddScoped<IAccountRepository, AccountRepository>();
		services.AddScoped<IAccountGroupRepository, AccountGroupRepository>();
		services.AddScoped<ICategoryRepository, CategoryRepository>();
		services.AddScoped<ICategoryGroupRepository, CategoryGroupRepository>();
		services.AddScoped<ICorrespondentRepository, CorrespondentRepository>();
		services.AddScoped<ICorrespondentGroupRepository, CorrespondentGroupRepository>();
		services.AddScoped<ICurrencyRepository, CurrencyRepository>();
		services.AddScoped<ICurrencyRateRepository, CurrencyRateRepository>();
		services.AddScoped<IProjectRepository, ProjectRepository>();
		services.AddScoped<IProjectGroupRepository, ProjectGroupRepository>();
		services.AddScoped<ITemplateRepository, TemplateRepository>();
		services.AddScoped<ITemplateEntryRepository, TemplateEntryRepository>();
		services.AddScoped<ITemplateGroupRepository, TemplateGroupRepository>();
		services.AddScoped<ITransactionRepository, TransactionRepository>();
		services.AddScoped<ITransactionEntryRepository, TransactionEntryRepository>();
		services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
		services.AddScoped<IUserConfigRepository, UserConfigRepository>();
	}
}
