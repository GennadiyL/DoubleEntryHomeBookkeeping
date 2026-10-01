namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Exposes a catalog name and optional description.
/// Descriptions are independent of names.
/// Services enforce the applicable trimming, nonblank and uniqueness rules.
/// Account names may be duplicated; classifications and groups have scoped uniqueness.
/// </summary>
public interface INamedEntity
{
	public string Name { get; set; }
	public string? Description { get; set; }
}
