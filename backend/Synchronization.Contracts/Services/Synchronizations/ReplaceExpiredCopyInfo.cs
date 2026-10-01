namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the registration and snapshot selected for expiry replacement.
/// Used to continue the explicit expired-copy download flow.
/// LocalDatasetKey identifies the replacement registration.
/// Transfer describes the authorized business snapshot.
/// The caller must retain the replacement registration identity.
/// Receiving this result is not successful local installation.
/// Local settings remain separate from synchronized business data.
/// Acknowledgement follows activation of the downloaded snapshot.
/// </summary>
public record ReplaceExpiredCopyInfo
{
	public required string LocalDatasetKey { get; set; }
	public required TransferInfo Transfer { get; set; }
}
