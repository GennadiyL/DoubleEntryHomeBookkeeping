using Exchange.Models.Synchronizations;

namespace Synchronization.Impl.Operations.CaptureChanges;

/// <summary>
/// Provides the implementation placeholder for ICaptureChangesOperation.
/// Registered with scoped lifetime in the Synchronization module.
/// Its Run signature reserves the agreed operation boundary.
/// Database behavior is intentionally not implemented in this increment.
/// Calling Run throws rather than returning a misleading successful result.
/// It performs no database mutation or durable file write.
/// Concrete dependencies will be introduced with the implementation.
/// The implementation remains internal to this project.
/// </summary>
internal sealed class CaptureChangesOperation : ICaptureChangesOperation
{
	public Task<SyncBatch> Run(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
