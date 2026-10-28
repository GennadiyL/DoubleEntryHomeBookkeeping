using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.EntityFramework.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

/// <summary>
/// Shares repository access and transaction behavior between database roles.
/// Derived units supply their context and an optional legacy repository key.
/// Repositories are resolved lazily through the current scoped service provider.
/// Factory-created units cache repositories constructed with their own context.
/// Repository writes and unit-of-work saves use the same role-specific context.
/// Common repository contracts remain independent of EF Core.
/// This base does not choose connection strings or create service providers.
/// Infrastructure owns registration and context disposal.
/// </summary>
public abstract class AppUnitOfWork : UnitOfWork<AppDbContext>, IAppUnitOfWork
{
	private readonly Type? _databaseKey;
	private readonly Dictionary<Type, object> _repositories = new();

	protected AppUnitOfWork(
		AppDbContext context,
		IServiceProvider serviceProvider,
		Type databaseKey) :
		base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
		_databaseKey = databaseKey;
	}

	protected AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider) :
		base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
	}

	private TRepository GetRepository<TRepository>() where TRepository : class
	{
		if (_databaseKey is not null)
		{
			return ServiceProvider.Value.GetRequiredKeyedService<TRepository>(_databaseKey);
		}
		if (!_repositories.TryGetValue(typeof(TRepository), out object? repository))
		{
			Func<AppDbContext, TRepository> create = ServiceProvider.Value.GetRequiredService<Func<AppDbContext, TRepository>>();
			repository = create(Context);
			_repositories.Add(typeof(TRepository), repository);
		}
		return (TRepository)repository;
	}

	public IAccountRepository AccountRepo => GetRepository<IAccountRepository>();
	public IAccountGroupRepository AccountGroupRepo => GetRepository<IAccountGroupRepository>();
	public ICategoryRepository CategoryRepo => GetRepository<ICategoryRepository>();
	public ICategoryGroupRepository CategoryGroupRepo => GetRepository<ICategoryGroupRepository>();
	public ICorrespondentRepository CorrespondentRepo => GetRepository<ICorrespondentRepository>();
	public ICorrespondentGroupRepository CorrespondentGroupRepo => GetRepository<ICorrespondentGroupRepository>();
	public ICurrencyRepository CurrencyRepo => GetRepository<ICurrencyRepository>();
	public ICurrencyRateRepository CurrencyRateRepo => GetRepository<ICurrencyRateRepository>();
	public IReportRepository ReportRepo => GetRepository<IReportRepository>();
	public IReportGroupRepository ReportGroupRepo => GetRepository<IReportGroupRepository>();
	public IProjectRepository ProjectRepo => GetRepository<IProjectRepository>();
	public IProjectGroupRepository ProjectGroupRepo => GetRepository<IProjectGroupRepository>();
	public ITemplateRepository TemplateRepo => GetRepository<ITemplateRepository>();
	public ITemplateEntryRepository TemplateEntryRepo => GetRepository<ITemplateEntryRepository>();
	public ITemplateGroupRepository TemplateGroupRepo => GetRepository<ITemplateGroupRepository>();
	public ITransactionRepository TransactionRepo => GetRepository<ITransactionRepository>();
	public ITransactionEntryRepository TransactionEntryRepo => GetRepository<ITransactionEntryRepository>();
	public ISystemConfigRepository SystemConfigRepo => GetRepository<ISystemConfigRepository>();
	public ILocalJsonRepository LocalJsonRepo => GetRepository<ILocalJsonRepository>();
	public ILocalConfigRepository LocalConfigRepo => GetRepository<ILocalConfigRepository>();
	public ILocalRegistrationRepository LocalRegistrationRepo => GetRepository<ILocalRegistrationRepository>();
	public ILocalSyncBatchRepository LocalSyncBatchRepo => GetRepository<ILocalSyncBatchRepository>();
}
