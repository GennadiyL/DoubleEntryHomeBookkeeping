using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Reporting;

/// <summary>
/// Represents a saved report definition independently of its display name.
/// Name labels the report, and Json stores calculation instructions rather than results.
/// Tracking fields support synchronization and deletion of the report definition.
/// Services interpret referenced catalog identities and change instructions through explicit saves.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class Report : BaseEntity, ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public required string Name { get; set; }
	public required string Json { get; set; }
}
