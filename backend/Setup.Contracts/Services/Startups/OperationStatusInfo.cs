namespace Setup.Contracts.Services.Startups;

/// <summary>
/// Describes an observable setup or synchronization outcome.
/// Used by the startup screen and synchronization progress UI.
/// State identifies the applicable stage; unrelated fields remain null.
/// Snapshot versions are signed 64-bit Master revisions.
/// MasterExists is null when existence is unknown.
/// LastSuccessAt is a UTC instant when present.
/// Message is user-facing outcome information, not diagnostic secrets.
/// This detached projection does not authorize access or persist state.
/// </summary>
public record OperationStatusInfo
{
	public OperationState State { get; set; }
	public bool? MasterExists { get; set; }
	public string? SyncKey { get; set; }
	public long? PublishedVersion { get; set; }
	public long? InstalledVersion { get; set; }
	public DateTime? LastSuccessAt { get; set; }
	public string? Message { get; set; }
}
