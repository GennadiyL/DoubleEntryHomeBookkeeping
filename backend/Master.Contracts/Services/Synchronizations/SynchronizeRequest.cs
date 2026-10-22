using Exchange.Models.Synchronizations;

namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Submits the typed pending-change document to the Master business service.
/// Container identifies the Master dataset and registered Local copy.
/// Its format and schema versions are validated before applying changes.
/// Batches retain their original keys, order and conflict priorities on retries.
/// Master skips batches whose immutable identities were already applied.
/// Successful processing covers every submitted batch in the container.
/// No separate attempt key, duplicated priority or opaque JSON is required.
/// Caller authorization is established by infrastructure before this request is used.
/// </summary>
public record SynchronizeRequest
{
	public required SyncContainer Container { get; set; }
}
