namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Returns a consistent Master database snapshot and its actual revision.
/// Revision identifies the state represented by Content, not a later live revision.
/// It must be greater than or equal to DownloadRequest.MinimumRevision.
/// Content contains the business database and excludes authentication secrets.
/// The caller owns and disposes the readable snapshot stream.
/// Transport adapters stream bytes separately from JSON metadata.
/// Client validates and installs the snapshot before acknowledging this revision.
/// Receiving the response alone does not confirm successful local installation.
/// </summary>
public record DownloadResponse
{
	public long Revision { get; set; }
	public required Stream Content { get; set; }
}
