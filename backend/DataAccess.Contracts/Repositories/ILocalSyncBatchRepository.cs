using Business.Models.Entities.Synchronizations;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ILocalSyncBatchRepository : IRepository<LocalSyncBatch>
{
	public Task<LocalSyncBatch?> GetBySyncKey(Guid localRegistrationId, string syncKey, CancellationToken cancellationToken = default);
}
