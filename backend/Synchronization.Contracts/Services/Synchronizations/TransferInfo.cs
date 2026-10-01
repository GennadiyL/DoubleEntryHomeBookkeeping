namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Identifies an authorized immutable business snapshot transfer.
/// Version is the selected Master snapshot revision.
/// TransferToken is an opaque authorization-scoped transfer reference.
/// IntegrityProof is optional pending the transfer protocol decision.
/// Used by setup, synchronization and recovery callers.
/// Snapshot bytes are returned separately by Download.
/// The descriptor never contains Admin.db or owner credentials.
/// Token encoding, expiry and integrity verification remain transport concerns.
/// </summary>
public record TransferInfo
{
	public long Version { get; set; }
	public required string TransferToken { get; set; }
	public string? IntegrityProof { get; set; }
}
