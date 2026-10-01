using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines explicit favorite selection for catalog entities.
/// Favorites are synchronized content rather than inherited hierarchy state.
/// Protected roots remain non-favorite.
/// Filtering and ancestor-path display do not alter persisted favorite values.
/// </summary>
public interface IFavoriteService<T>
	where T : IFavoriteEntity
{
	/// <summary>
	/// Saves an explicit favorite selection for catalog browsing and filtering.
	/// Requires a live target and rejects protected root groups, which remain non-favorite.
	/// An unchanged selection returns without saving; changes do not cascade to descendants.
	/// Adds Content while preserving Order and received revisions, and saves the action atomically.
	/// </summary>
	public Task SetFavoriteStatus(Guid entityId, bool isFavorite, CancellationToken cancellationToken = default);
}
