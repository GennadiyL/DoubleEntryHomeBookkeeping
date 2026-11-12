using Exchange.Models.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.MergeSyncBatch;

/// <summary>
/// Holds input references and working indexes for one merge invocation.
/// Batch preserves the submitted immutable change document.
/// Sets contains detached entity state shared with subsequent operations.
/// Sources identifies the Local responsible for each accepted revision.
/// Identities and RateKeys detect duplicates within the current batch.
/// Rates indexes existing and newly accepted currency rates by natural key.
/// The merge operation creates this data object for each call and retains none of it.
/// No callbacks, repository references or business methods are stored here.
/// </summary>
internal sealed record SyncBatchMergeState
{
	public required SyncBatch Batch { get; init; }
	public required Guid LocalRegistrationId { get; init; }
	public required long Revision { get; init; }
	public required Dictionary<string, SyncEntityState> Sets { get; init; }
	public required Dictionary<long, Guid> Sources { get; init; }
	public HashSet<(string Type, Guid Id)> Identities { get; } = new();
	public HashSet<(Guid CurrencyId, DateOnly Date)> RateKeys { get; } = new();
	public Dictionary<(Guid CurrencyId, DateOnly Date), SyncMergeRow> Rates { get; } = new();
}
