namespace Dehb.WebApi.Params;

public record CombineElementsParam
{
	public Guid ToElementId { get; set; }
	public Guid FromElementId { get; set; }
}
