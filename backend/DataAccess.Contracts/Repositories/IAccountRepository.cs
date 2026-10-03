using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface IAccountRepository : IElementRepository<AccountGroup, Account>
{
	public Task<ICollection<Account>> GetByCategoryId(Guid categoryId, CancellationToken cancellationToken = default);
	public Task<ICollection<Account>> GetByCorrespondentId(Guid correspondentId, CancellationToken cancellationToken = default);
	public Task<ICollection<Account>> GetByProjectId(Guid projectId, CancellationToken cancellationToken = default);
	public Task<bool> HasByCurrencyId(Guid currencyId, CancellationToken cancellationToken = default);
}
