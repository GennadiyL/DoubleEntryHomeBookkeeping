using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface IAccountRepository : IElementRepository<AccountGroup, Account>
{
	public Task<ICollection<Account>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
	public Task<ICollection<Account>> GetByCorrespondentIdAsync(Guid correspondentId, CancellationToken cancellationToken = default);
	public Task<ICollection<Account>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
	public Task<bool> HasByCurrencyIdAsync(Guid currencyId, CancellationToken cancellationToken = default);
}
