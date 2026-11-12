using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories.Base;

/// <summary>
/// Reads and accepts independently tracked entities at the local batch boundary.
/// Extends ordinary persistence with selection, bulk deletion and tracking acceptance.
/// New entities qualify even when their modification flags are empty.
/// Subsequent content or ordering flags also qualify an entity.
/// Rows with DeleteRevision zero qualify independently of modification flags.
/// Already captured changes are represented by their durable batches.
/// Aggregate repositories load the owned entries needed for capture.
/// HardDelete removes rows with a zero deletion revision, including database cascades.
/// AcceptChanges sets all captured creation revisions to zero and clears flags, including deleted creations.
/// Mutations execute immediately and return affected root-row counts; no SaveChanges is needed.
/// The caller secures JSON, hard-deletes captured deletions and accepts surviving tracking in one transaction.
/// Use a fresh tracking scope; bulk SQL does not refresh already tracked entities.
/// </summary>
public interface ITrackedRepository<T> : IRepository<T>
	where T : class, IBaseEntity, ITrackedEntity
{
	public Task<ICollection<T>> GetChanges(CancellationToken cancellationToken = default);

	public Task<int> HardDelete(CancellationToken cancellationToken = default);

	public Task<int> AcceptChanges(CancellationToken cancellationToken = default);
}
