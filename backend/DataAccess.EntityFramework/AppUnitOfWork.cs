using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.EntityFramework.Entities;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework;

/// <summary>
/// Shares repository access and transaction behavior between database roles.
/// Derived units supply their factory-owned context.
/// Repositories are resolved lazily through the current scoped service provider.
/// Factory-created units cache repositories constructed with their own context.
/// Repository writes and unit-of-work saves use the same role-specific context.
/// Common repository contracts remain independent of EF Core.
/// This base does not choose connection strings or create service providers.
/// Shared disposal closes the context for every database role.
/// </summary>
public abstract class AppUnitOfWork : UnitOfWork<AppDbContext>, IAppUnitOfWork, IEntityFrameworkUnitOfWork, IDisposable
{
	private readonly Dictionary<Type, object> _repositories = new();

	protected AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider) :
		base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
	}

	private TRepository GetRepository<TRepository>() where TRepository : class
	{
		if (!_repositories.TryGetValue(typeof(TRepository), out object? repository))
		{
			Func<AppDbContext, TRepository> create = ServiceProvider.Value.GetRequiredService<Func<AppDbContext, TRepository>>();
			repository = create(Context);
			_repositories.Add(typeof(TRepository), repository);
		}
		return (TRepository)repository;
	}
	public DatabaseFacade Database => Context.Database;

	public void Dispose()
	{
		Context.Dispose();
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await Context.DisposeAsync();
		GC.SuppressFinalize(this);
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
