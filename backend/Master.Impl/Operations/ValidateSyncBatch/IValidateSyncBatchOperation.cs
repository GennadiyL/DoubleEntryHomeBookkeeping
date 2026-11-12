using Business.Models.Entities.Config;
using Exchange.Models.Enums;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.ValidateSyncBatch;

/// <summary>
/// Validates and normalizes the supplied detached Master state after restoration.
/// Receives configuration and conflict preference for each invocation.
/// Repairs hierarchy cycles and sibling names using that preference.
/// Normalizes accounting values and independent catalog ordering.
/// Mutates supplied rows without retaining them between calls.
/// Invalid content prevents the caller from committing the batch.
/// Revision stamping and persistence remain the caller's responsibilities.
/// Implementations are replaceable through Master dependency injection.
/// </summary>
internal interface IValidateSyncBatchOperation
{
	/// <summary>
	/// Validates and normalizes the restored graph before revision stamping and persistence.
	/// </summary>
	public void Run(Dictionary<string, SyncEntityState> sets, SystemConfig config, ConflictPriority priority, CancellationToken cancellationToken = default);
}
