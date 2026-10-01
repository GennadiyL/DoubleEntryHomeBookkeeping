namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Exposes the explicit favorite selection of a catalog entity.
/// The default is false, and protected roots always remain false.
/// Favorite status is not inherited or cascaded to descendants.
/// Changes synchronize as content rather than order.
/// </summary>
public interface IFavoriteEntity
{
	public bool IsFavorite { get; set; }
}
