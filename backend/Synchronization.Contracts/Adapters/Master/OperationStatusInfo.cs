namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Returns the server-observable result of a synchronization operation.
/// State distinguishes pending publication from acknowledged completion.
/// SyncKey identifies the durable attempt when applicable.
/// PublishedVersion identifies the immutable Master snapshot produced.
/// InstalledVersion is the version acknowledged by the Local copy.
/// LastSuccessAt uses the Master UTC clock when success is recorded.
/// Message contains user-facing information without credentials or secrets.
/// Absent values must not be interpreted as successful completion.
/// </summary>
public record OperationStatusInfo
{
	public OperationState State { get; set; }
	public string? SyncKey { get; set; }
	public long? PublishedVersion { get; set; }
	public long? InstalledVersion { get; set; }
	public DateTime? LastSuccessAt { get; set; }
	public string? Message { get; set; }
}
