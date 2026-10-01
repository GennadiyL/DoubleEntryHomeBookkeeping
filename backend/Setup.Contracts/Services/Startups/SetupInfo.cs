namespace Setup.Contracts.Services.Startups;

/// <summary>
/// Returns dataset registration and authorized initial snapshot metadata.
/// Used after CreateBooks or OpenBooks to complete setup.
/// MasterDatasetKey identifies the owner business dataset.
/// LocalDatasetKey identifies the registered local copy.
/// AuthorizationHandle is opaque and must be stored securely, never logged.
/// Transfer selects the complete business snapshot to obtain.
/// Cloud setup success does not imply successful local installation.
/// No password hash or administration database is exposed.
/// </summary>
public record SetupInfo
{
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
	public required string AuthorizationHandle { get; set; }
	public required TransferInfo Transfer { get; set; }
}
