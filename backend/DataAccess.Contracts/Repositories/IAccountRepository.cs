using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface IAccountRepository : IElementRepository<AccountGroup, Account>
{
	public Task<ICollection<Account>> GetByCategoryIdAsync(Guid categoryId);
	public Task<ICollection<Account>> GetByCorrespondentIdAsync(Guid correspondentId);
	public Task<ICollection<Account>> GetByProjectIdAsync(Guid projectId);
}

