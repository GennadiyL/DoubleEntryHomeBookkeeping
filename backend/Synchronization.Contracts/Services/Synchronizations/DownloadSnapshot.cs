namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Selects an immutable snapshot for an authorized download.
/// Used after receiving a transfer descriptor.
/// Version and TransferToken must identify the same snapshot.
/// LocalDatasetKey supplies the owned registration scope.
/// Owner authentication remains separate from the payload.
/// Partial transfers restart under the TRD recovery rules.
/// The result contains business snapshot bytes only.
/// This selection does not acknowledge successful local installation.
/// </summary>
public record DownloadSnapshot
{
	public required string LocalDatasetKey { get; set; }
	public long Version { get; set; }
	public required string TransferToken { get; set; }
}
