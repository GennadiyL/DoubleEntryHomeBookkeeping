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

	public async Task<ICollection<AccountEntity>> GetByCategoryIdAsync(Guid categoryId)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.CategoryId == categoryId).ToListAsync();
		return Mapper.Map<Account, AccountEntity>(accounts);
	}

	public async Task<ICollection<AccountEntity>> GetByCorrespondentIdAsync(Guid correspondentId)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.CorrespondentId == correspondentId).ToListAsync();
		return Mapper.Map<Account, AccountEntity>(accounts);
	}

	public async Task<ICollection<AccountEntity>> GetByProjectIdAsync(Guid projectId)
	{
		List<Account> accounts = await Entities.AsNoTracking()
			.Where(account => account.ProjectId == projectId).ToListAsync();
		return Mapper.Map<Account, AccountEntity>(accounts);
	}
}
