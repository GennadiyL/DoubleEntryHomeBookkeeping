using Business.Core.Entities;
using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities.Base;

/// <summary>
/// Defines the common state of named catalog entities.
/// It extends persistent identity with tracking, ordering, and presentation data.
/// Derived catalog models expose a writable identifier inherited from BaseEntity.
/// Identifiers can be assigned when instances are created or materialized.
/// The class contains persistent state only and does not implement business workflows.
/// </summary>
public abstract class CatalogEntity : BaseEntity, ICatalogEntity
{
	public DateTime Original { get; set; }
	public DateTime Current { get; set; }
	public bool IsDeleted { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
}
