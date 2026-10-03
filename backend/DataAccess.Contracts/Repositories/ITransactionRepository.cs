using Business.Models.Entities;
using DataAccess.Contracts.Queries;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

/// <summary>
/// Persists transaction parent records and reads complete aggregates.
/// Business supplies validated selection criteria.
/// Searches return detached business entities with loaded entry accounts.
/// The database applies membership and count limits before graph loading.
/// Entries remain ordered by their stored position.
/// Mutation validation and synchronization decisions belong to Business.
/// Writes are staged in the caller's unit of work.
/// Repository methods do not own transaction lifetime.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
	public Task<ICollection<Transaction>> Search(TransactionSearch search, CancellationToken cancellationToken = default);
}
