using Business.Models.Entities.Base;

namespace Business.Models.Entities.Reporting;

/// <summary>
/// Represents a persistent group in the saved-report catalog.
/// Uses the same group state and relationships as other catalog families.
/// Parent and child groups belong exclusively to the report hierarchy.
/// Elements contains the reports directly assigned to this group.
/// The fixed root is self-parented and excluded from its own children.
/// Inherited fields provide naming, favorites, ordering and synchronization.
/// Services own validation, movement, merging and deletion behavior.
/// This parameterless model contains persistent state without report calculations.
/// </summary>
public class ReportGroup : GroupEntity<ReportGroup, Report>
{
}
