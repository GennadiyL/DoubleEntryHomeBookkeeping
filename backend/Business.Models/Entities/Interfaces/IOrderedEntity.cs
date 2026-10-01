namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Exposes a catalog position within its applicable sequence.
/// Accepted positions are consecutive and zero-based.
/// Equal positions are resolved using canonical lowercase GUID strings compared ordinally.
/// Local position edits set Order without adding Content or advancing EditRevision.
/// Transaction and template entry Position fields use their parent content rules instead.
/// </summary>
public interface IOrderedEntity
{
	public int Order { get; set; }
}
