namespace Business.Contracts.Base.Params;

/// <summary>
/// Supplies an explicit favorite value for an editor save.
/// The value applies only to the edited entity without descendant inheritance.
/// New input defaults to false; root groups must remain non-favorite.
/// </summary>
public interface IFavoriteParam
{
	public bool IsFavorite { get; set; }
}
