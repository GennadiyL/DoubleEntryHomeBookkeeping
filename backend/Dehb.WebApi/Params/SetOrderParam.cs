namespace Dehb.WebApi.Params;

public record SetOrderParam
{
	public Guid EntityId { get; set; }
	public int Order { get; set; }
}
