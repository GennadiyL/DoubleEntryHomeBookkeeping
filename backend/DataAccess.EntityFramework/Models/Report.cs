using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

/// <summary>
/// Stores the report catalog state used by Entity Framework.
/// Implements the DAL identity contract for repository persistence.
/// Scalar fields mirror the corresponding persistent business model.
/// Navigation properties describe only report catalog relationships.
/// The mapper translates loaded relationships without additional queries.
/// Repositories expose business entities instead of this internal type.
/// Business services remain responsible for validation and lifecycle rules.
/// This model does not calculate reports or interpret saved instructions.
/// </summary>
internal class Report : IDalEntity
{
	public string Json { get; set; } = string.Empty;
	public Guid Id { get; set; }

	public long? EditRevision { get; set; }

	public long? DeleteRevision { get; set; }

	public ModificationType ModificationType { get; set; }

	public string Name { get; set; } = string.Empty;

	public string? Description { get; set; }

	public int Order { get; set; }

	public bool IsFavorite { get; set; }

	public ReportGroup? Group { get; set; }

	public Guid GroupId { get; set; }
}
