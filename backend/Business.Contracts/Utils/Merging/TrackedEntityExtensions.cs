using Business.Models.Constants;
using System.ComponentModel;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Contracts.Utils.Merging;

/// <summary>
/// Provides predicates and field setters for tracked entity state.
/// Live entities with null EditRevision are recognized as new.
/// Submitted or accepted live entities are classified using modification flags.
/// A non-null DeleteRevision identifies a deleted entity.
/// Content and Order setters add their bit without clearing existing flags.
/// Deletion and restoration preserve the existing Content and Order flags.
/// Capture acceptance removes deleted rows after securing their immutable batch.
/// Individual setters do not initialize all three tracking properties.
/// These helpers do not determine all pending synchronization work or manage outgoing batches.
/// Services remain responsible for atomic persistence, soft deletion and eligible delta cleanup.
/// </summary>
public static class TrackedEntityExtensions
{
	public static bool IsNew(this ITrackedEntity entity) => entity.EditRevision == null && entity.DeleteRevision == null;
	public static bool IsUnchanged(this ITrackedEntity entity) => entity.IsAliveAndExisted() && entity.ModificationType == 0;
	public static bool IsEdited(this ITrackedEntity entity) => entity.IsAliveAndExisted() && entity.ModificationType != 0;
	public static bool IsEditedContent(this ITrackedEntity entity) =>
		entity.IsAliveAndExisted() && (entity.ModificationType & ModificationType.Content) != 0;
	public static bool IsEditedOrder(this ITrackedEntity entity) =>
		entity.IsAliveAndExisted() && (entity.ModificationType & ModificationType.Order) != 0;
	public static bool IsDeleted(this ITrackedEntity entity) => entity.DeleteRevision != null;

	public static void SetNew(this ITrackedEntity entity) => entity.EditRevision = null;
	public static void SetEditedContent(this ITrackedEntity entity) => entity.ModificationType |= ModificationType.Content;
	public static void SetEditedOrder(this ITrackedEntity entity) => entity.ModificationType |= ModificationType.Order;
	public static void SetDeleted(this ITrackedEntity entity) => entity.DeleteRevision = AppValues.LocalDeleteRevision;
	public static void SetAlive(this ITrackedEntity entity) => entity.DeleteRevision = null;

	private static bool IsAliveAndExisted(this ITrackedEntity entity) => entity is { EditRevision: not null, DeleteRevision: null };
}
