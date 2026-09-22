using DataAccess.Contracts.Repositories;
using DataAccess.Core.Entities;

namespace DataAccess.Contracts;

public interface IAppUnitOfWork : IUnitOfWork
{
	public IAccountRepository AccountRepo { get; }

	public IAccountGroupRepository AccountGroupRepo { get; }

	public ICategoryRepository CategoryRepo { get; }

	public ICategoryGroupRepository CategoryGroupRepo { get; }

	public ICorrespondentRepository CorrespondentRepo { get; }

	public ICorrespondentGroupRepository CorrespondentGroupRepo { get; }

	public ICurrencyRepository CurrencyRepo { get; }

	public ICurrencyRateRepository CurrencyRateRepo { get; }

	public IProjectRepository ProjectRepo { get; }

	public IProjectGroupRepository ProjectGroupRepo { get; }

	public ITemplateRepository TemplateRepo { get; }

	public ITemplateEntryRepository TemplateEntryRepo { get; }

	public ITemplateGroupRepository TemplateGroupRepo { get; }

	public ITransactionRepository TransactionRepo { get; }

	public ITransactionEntryRepository TransactionEntryRepo { get; }

	public ISystemConfigRepository SystemConfigRepo { get; }

	public IUserConfigRepository UserConfigRepo { get; }
}
