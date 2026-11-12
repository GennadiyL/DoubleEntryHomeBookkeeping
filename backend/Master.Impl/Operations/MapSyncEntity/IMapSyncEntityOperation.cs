using System.Text.Json;
using Business.Core.Entities;
using Exchange.Models.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.MapSyncEntity;

/// <summary>
/// Converts explicitly registered synchronization entity content.
/// Receives the mapping schema and entity values through method arguments.
/// Reads payload fields into anemic business entities.
/// Produces detached content snapshots for merge comparison.
/// Includes owned entries while excluding navigation and derived cache data.
/// Never loads repositories, saves entities or retains invocation state.
/// The caller supplies only trusted mapping schemas declared by Master.
/// Implementations can be substituted independently of merge policy.
/// </summary>
internal interface IMapSyncEntityOperation
{
	/// <summary>
	/// Reads allowed payload fields into an entity used by conflict resolution.
	/// </summary>
	public BaseEntity Read(SyncEntityState set, SyncObject item, bool content, bool order);

	/// <summary>
	/// Captures business content for original-state comparison during batch processing.
	/// </summary>
	public Dictionary<string, JsonElement> Snapshot(SyncEntityState set, BaseEntity entity);

	/// <summary>
	/// Records owned entry content after the caller attaches loaded aggregate entries.
	/// </summary>
	public void RememberAggregate(SyncEntityState set, SyncMergeRow row);

	/// <summary>
	/// Determines whether accepted content requires a new revision or aggregate persistence.
	/// </summary>
	public bool ContentChanged(SyncEntityState set, SyncMergeRow row);
}
