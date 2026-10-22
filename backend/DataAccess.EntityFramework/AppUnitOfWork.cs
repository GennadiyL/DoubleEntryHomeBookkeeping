using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.EntityFramework.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

/// <summary>
/// Shares repository access and transaction behavior between database roles.
/// Derived units of work supply their context and repository service key.
/// Every repository is resolved lazily from the current dependency scope.
/// Keyed resolution keeps Local and Master repository instances separate.
/// Repository writes and unit-of-work saves use the same role-specific context.
/// Common repository contracts remain independent of EF Core.
/// This base does not choose connection strings or create service providers.
/// Infrastructure owns registration and context disposal.
/// </summary>
public abstract class AppUnitOfWork : UnitOfWork<AppDbContext>, IAppUnitOfWork
{
	private readonly Type _databaseKey;

	protected AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider, Type databaseKey)
		: base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
		_databaseKey = databaseKey;
	}

	public IAccountRepository AccountRepo => ServiceProvider.Value.GetRequiredKeyedService<IAccountRepository>(_databaseKey);
	public IAccountGroupRepository AccountGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<IAccountGroupRepository>(_databaseKey);
	public ICategoryRepository CategoryRepo => ServiceProvider.Value.GetRequiredKeyedService<ICategoryRepository>(_databaseKey);
	public ICategoryGroupRepository CategoryGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<ICategoryGroupRepository>(_databaseKey);
	public ICorrespondentRepository CorrespondentRepo => ServiceProvider.Value.GetRequiredKeyedService<ICorrespondentRepository>(_databaseKey);
	public ICorrespondentGroupRepository CorrespondentGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<ICorrespondentGroupRepository>(_databaseKey);
	public ICurrencyRepository CurrencyRepo => ServiceProvider.Value.GetRequiredKeyedService<ICurrencyRepository>(_databaseKey);
	public ICurrencyRateRepository CurrencyRateRepo => ServiceProvider.Value.GetRequiredKeyedService<ICurrencyRateRepository>(_databaseKey);
	public IReportRepository ReportRepo => ServiceProvider.Value.GetRequiredKeyedService<IReportRepository>(_databaseKey);
	public IReportGroupRepository ReportGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<IReportGroupRepository>(_databaseKey);
	public IProjectRepository ProjectRepo => ServiceProvider.Value.GetRequiredKeyedService<IProjectRepository>(_databaseKey);
	public IProjectGroupRepository ProjectGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<IProjectGroupRepository>(_databaseKey);
	public ITemplateRepository TemplateRepo => ServiceProvider.Value.GetRequiredKeyedService<ITemplateRepository>(_databaseKey);
	public ITemplateEntryRepository TemplateEntryRepo => ServiceProvider.Value.GetRequiredKeyedService<ITemplateEntryRepository>(_databaseKey);
	public ITemplateGroupRepository TemplateGroupRepo => ServiceProvider.Value.GetRequiredKeyedService<ITemplateGroupRepository>(_databaseKey);
	public ITransactionRepository TransactionRepo => ServiceProvider.Value.GetRequiredKeyedService<ITransactionRepository>(_databaseKey);
	public ITransactionEntryRepository TransactionEntryRepo => ServiceProvider.Value.GetRequiredKeyedService<ITransactionEntryRepository>(_databaseKey);
	public ISystemConfigRepository SystemConfigRepo => ServiceProvider.Value.GetRequiredKeyedService<ISystemConfigRepository>(_databaseKey);
	public ILocalConfigRepository LocalConfigRepo => ServiceProvider.Value.GetRequiredKeyedService<ILocalConfigRepository>(_databaseKey);
	public ILocalRegistrationRepository LocalRegistrationRepo => ServiceProvider.Value.GetRequiredKeyedService<ILocalRegistrationRepository>(_databaseKey);
	public ILocalSyncBatchRepository LocalSyncBatchRepo => ServiceProvider.Value.GetRequiredKeyedService<ILocalSyncBatchRepository>(_databaseKey);
}
