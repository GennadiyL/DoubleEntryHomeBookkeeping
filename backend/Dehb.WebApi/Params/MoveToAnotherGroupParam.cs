namespace Dehb.WebApi.Params;

public record MoveToAnotherGroupParam
{
	public Guid EntityId { get; set; }
	public Guid ToGroupId { get; set; }
}
