namespace Dehb.WebApi.Params;

public record SetFavoriteStatusParam
{
	public Guid EntityId { get; set; }
	public bool IsFavorite { get; set; }
}
