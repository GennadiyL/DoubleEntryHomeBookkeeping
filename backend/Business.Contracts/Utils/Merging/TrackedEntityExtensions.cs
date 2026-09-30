using System.ComponentModel;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Contracts.Utils.Merging;

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
	public static void SetDeleted(this ITrackedEntity entity) => entity.DeleteRevision = 0;
	public static void SetAlive(this ITrackedEntity entity) => entity.DeleteRevision = null;

	private static bool IsAliveAndExisted(this ITrackedEntity entity) => entity is { EditRevision: not null, DeleteRevision: null };
}
