using System.Text.Json;
using Business.Core.Entities;
using Business.Models.Entities.Interfaces;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Holds a detached entity while one Master batch is reconciled.
/// Original scalar content is retained for final change detection.
/// Original metadata distinguishes insertion, restoration and unchanged state.
/// Incoming content preference is retained for hierarchy and name corrections.
/// Order is captured independently of content and revision calculation.
/// Instances exist only during a single operation invocation.
/// They neither track EF entities nor manage database transactions.
/// Navigation graphs and derived cumulative values are not compared as content.
/// </summary>
internal sealed record SyncMergeRow
{
	public required BaseEntity Entity { get; set; }
	public Dictionary<string, JsonElement>? Original { get; init; }
	public long? OriginalEditRevision { get; init; }
	public long? OriginalDeleteRevision { get; init; }
	public int? OriginalOrder { get; init; }
	public bool IncomingContent { get; set; }
	public ITrackedEntity Tracking => (ITrackedEntity)Entity;
	public bool IsNew => Original is null;
	public bool IsLive => Tracking.DeleteRevision is null;
}
