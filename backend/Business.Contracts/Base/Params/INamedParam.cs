namespace Business.Contracts.Base.Params;

public interface INamedParam
{
	public string Name { get; set; }
	public string? Description { get; set; }
}
