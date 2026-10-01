namespace Dehb.WebApi.Params;

public record CombineGroupsParam
{
	public Guid ToGroupId { get; set; }
	public Guid FromGroupId { get; set; }
}
