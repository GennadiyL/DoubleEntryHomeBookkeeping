using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.EntityFramework.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

internal class AppUnitOfWork : UnitOfWork<AppDbContext>, IAppUnitOfWork
{
	public AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider)
		: base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
	}

	public IAccountRepository AccountRepo => ServiceProvider.Value.GetRequiredService<IAccountRepository>();
	public IAccountGroupRepository AccountGroupRepo => ServiceProvider.Value.GetRequiredService<IAccountGroupRepository>();
	public ICategoryRepository CategoryRepo => ServiceProvider.Value.GetRequiredService<ICategoryRepository>();
	public ICategoryGroupRepository CategoryGroupRepo => ServiceProvider.Value.GetRequiredService<ICategoryGroupRepository>();
	public ICorrespondentRepository CorrespondentRepo => ServiceProvider.Value.GetRequiredService<ICorrespondentRepository>();
	public ICorrespondentGroupRepository CorrespondentGroupRepo => ServiceProvider.Value.GetRequiredService<ICorrespondentGroupRepository>();
	public ICurrencyRepository CurrencyRepo => ServiceProvider.Value.GetRequiredService<ICurrencyRepository>();
	public ICurrencyRateRepository CurrencyRateRepo => ServiceProvider.Value.GetRequiredService<ICurrencyRateRepository>();
	public IProjectRepository ProjectRepo => ServiceProvider.Value.GetRequiredService<IProjectRepository>();
	public IProjectGroupRepository ProjectGroupRepo => ServiceProvider.Value.GetRequiredService<IProjectGroupRepository>();
	public ITemplateRepository TemplateRepo => ServiceProvider.Value.GetRequiredService<ITemplateRepository>();
	public ITemplateEntryRepository TemplateEntryRepo => ServiceProvider.Value.GetRequiredService<ITemplateEntryRepository>();
	public ITemplateGroupRepository TemplateGroupRepo => ServiceProvider.Value.GetRequiredService<ITemplateGroupRepository>();
	public ITransactionRepository TransactionRepo => ServiceProvider.Value.GetRequiredService<ITransactionRepository>();
	public ITransactionEntryRepository TransactionEntryRepo => ServiceProvider.Value.GetRequiredService<ITransactionEntryRepository>();
	public ISystemConfigRepository SystemConfigRepo => ServiceProvider.Value.GetRequiredService<ISystemConfigRepository>();
	public IUserConfigRepository UserConfigRepo => ServiceProvider.Value.GetRequiredService<IUserConfigRepository>();
}
