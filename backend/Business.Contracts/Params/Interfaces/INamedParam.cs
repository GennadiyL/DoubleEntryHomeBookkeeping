namespace Business.Contracts.Params.Interfaces;

public interface INamedParam
{
	public string Name { get; set; }
	public string? Description { get; set; }
}
