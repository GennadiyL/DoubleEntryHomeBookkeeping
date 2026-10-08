using Business.Models.Entities.Base;

namespace Business.Models.Entities.Reporting;

/// <summary>
/// Stores saved report instructions within the report catalog hierarchy.
/// Inherits stable identity, naming, order, favorites and content tracking.
/// GroupId and Group identify the report's required owning group.
/// Json preserves calculation instructions rather than calculated results.
/// Report services interpret those instructions and validate explicit saves.
/// Moving or renaming a report does not rewrite its instructions.
/// The parameterless model supports disconnected reads and materialization.
/// Calculation, export and user-interface behavior remain outside this model.
/// </summary>
public class Report : ElementEntity<ReportGroup, Report>
{
	public string Json { get; set; } = string.Empty;
}
