namespace Dehb.WebApi.Params;

public record MoveToAnotherParentParam
{
	public Guid GroupId { get; set; }
	public Guid ToParentId { get; set; }
}
