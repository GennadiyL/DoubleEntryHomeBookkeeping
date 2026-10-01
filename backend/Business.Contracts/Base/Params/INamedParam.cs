namespace Business.Contracts.Base.Params;

/// <summary>
/// Supplies a catalog name and optional independent description.
/// Services trim and validate the name under the concrete entity rules.
/// Reference labels and synchronization state are not part of this input.
/// </summary>
public interface INamedParam
{
	public string Name { get; set; }
	public string? Description { get; set; }
}
