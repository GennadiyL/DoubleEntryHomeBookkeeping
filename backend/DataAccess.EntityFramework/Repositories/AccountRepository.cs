using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using AccountGroupEntity = Business.Models.Entities.AccountGroup;
using AccountEntity = Business.Models.Entities.Account;

namespace DataAccess.EntityFramework.Repositories;

internal sealed class AccountRepository : ElementRepository<AccountGroupEntity, AccountEntity, Account>, IAccountRepository
{
	public AccountRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public async Task<ICollection<AccountEntity>> GetByCategoryId(Guid categoryId, CancellationToken cancellationToken = default)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.CategoryId == categoryId).ToListAsync(cancellationToken);
		return Mapper.Map<Account, AccountEntity>(accounts);
	}

	public async Task<ICollection<AccountEntity>> GetByCorrespondentId(Guid correspondentId, CancellationToken cancellationToken = default)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.CorrespondentId == correspondentId).ToListAsync(cancellationToken);
		return Mapper.Map<Account, AccountEntity>(accounts);
	}

	public async Task<ICollection<AccountEntity>> GetByProjectId(Guid projectId, CancellationToken cancellationToken = default)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.ProjectId == projectId).ToListAsync(cancellationToken);
		return Mapper.Map<Account, AccountEntity>(accounts);
	}

	public Task<bool> HasByCurrencyId(Guid currencyId, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking().AnyAsync(account => account.CurrencyId == currencyId, cancellationToken);
}
