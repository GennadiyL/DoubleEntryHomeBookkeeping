using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories.Base;

/// <summary>
/// Reads independently tracked entities awaiting local batch capture.
/// Extends ordinary persistence with a synchronization selection query.
/// New entities qualify even when their modification flags are empty.
/// Subsequent content or ordering flags also qualify an entity.
/// Soft-deleted rows remain eligible under the same selection rule.
/// Already captured changes are represented by their durable batches.
/// Aggregate repositories load the owned entries needed for capture.
/// Reads do not accept changes or alter synchronization metadata.
/// </summary>
public interface ITrackedRepository<T> : IRepository<T>
	where T : class, IBaseEntity, ITrackedEntity
{
	public Task<ICollection<T>> GetChanges(CancellationToken cancellationToken = default);
}
