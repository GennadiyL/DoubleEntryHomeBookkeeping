using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines manual ordering within a catalog sequence.
/// Group children, direct elements and the currency catalog use independent sequences.
/// Positions are zero-based and consecutive after normalization.
/// Order changes are tracked independently from content and committed atomically.
/// </summary>
public interface IOrderedService<T>
	where T : IOrderedEntity
{
	/// <summary>
	/// Changes a live catalog member position for drag-and-drop ordering.
	/// Uses zero-based positions in its sibling sequence; groups and elements have separate sequences.
	/// Excludes the self-parent root and rejects attempts to reorder a protected root.
	/// Normalizes affected positions and adds Order only to rows whose numeric position changes.
	/// Preserves content flags and revisions; all changed rows commit together.
	/// </summary>
	public Task SetOrder(Guid entityId, int order, CancellationToken cancellationToken = default);
}
