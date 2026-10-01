using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Utils.Ordering;

/// <summary>
/// Provides collection helpers for the existing in-memory ordering implementation.
/// Reorder sorts by current Order and assigns zero-based positions.
/// SetOrder moves an existing member to its requested zero-based index and normalizes positions.
/// GetMaxOrder returns minus one for an empty collection so appending starts at zero.
/// The input collection determines which entities participate.
/// The helpers directly modify Order on the supplied objects.
/// They do not assign synchronization flags or update entity revisions.
/// Loading, validation and persistence remain the responsibility of the caller.
/// </summary>
public static class OrderingExtensions
{
	public static void Reorder<T>(this ICollection<T> entities) where T : class, IOrderedEntity
	{
		List<T> orderedItems = [..entities.OrderBy(i => i.Order)];

		for (int i = 0; i < orderedItems.Count; i++)
		{
			orderedItems[i].Order = i;
		}
	}

	public static void SetOrder<T>(this ICollection<T> entities, T orderedEntity, int order) where T : class, IOrderedEntity
	{
		ArgumentOutOfRangeException.ThrowIfNegative(order);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(order, entities.Count);
		List<T> orderedItems = [..entities.OrderBy(item => item.Order)];
		int currentIndex = orderedItems.FindIndex(item => ReferenceEquals(item, orderedEntity));
		if (currentIndex < 0)
		{
			throw new ArgumentException("The entity is not a member of this collection.", nameof(orderedEntity));
		}
		orderedItems.RemoveAt(currentIndex);
		orderedItems.Insert(order, orderedEntity);
		for (int index = 0; index < orderedItems.Count; index++)
		{
			orderedItems[index].Order = index;
		}
	}

	public static int GetMaxOrder<T>(this ICollection<T> entities) where T : IOrderedEntity =>
		entities.Count == 0 ? -1 : entities.Max(i => i.Order);
}
