namespace Dehb.WebApi.Params;

public class SetFavoriteStatusParam
{
	public Guid EntityId { get; set; }
	public bool IsFavorite { get; set; }
}
