namespace Business.Contracts.Base.Params;

/// <summary>
/// Supplies the parent identity for a group save.
/// Services require a live same-type parent and prevent hierarchy cycles.
/// Protected root groups cannot be edited through this input.
/// </summary>
public interface IGroupParam
{
	public Guid ParentId { get; set; }
}
