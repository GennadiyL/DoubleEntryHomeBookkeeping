namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns snapshot metadata and its readable byte stream.
/// Used by the local snapshot installer after Download.
/// Content contains the selected business database only.
/// The caller owns and disposes the returned stream.
/// The transport adapter must stream bytes outside JSON serialization.
/// Transfer identifies the immutable snapshot represented by Content.
/// Installation and integrity checks precede acknowledgement.
/// A returned stream does not imply a completed or activated download.
/// </summary>
public record DownloadSnapshotInfo
{
	public required TransferInfo Transfer { get; set; }
	public required Stream Content { get; set; }
}
