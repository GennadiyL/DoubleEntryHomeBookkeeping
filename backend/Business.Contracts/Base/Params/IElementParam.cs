namespace Business.Contracts.Base.Params;

/// <summary>
/// Supplies the target group identity for an element save.
/// Services require a live group in the matching catalog family.
/// The input exposes identity rather than a persistent navigation object.
/// </summary>
public interface IElementParam
{
	public Guid GroupId { get; set; }
}
