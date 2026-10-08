using Business.Core.Entities;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

/// <summary>
/// Stores the single configuration row belonging to one local database copy.
/// Local generates its row identity once and preserves it through database replacement.
/// LocalDatasetKey identifies the registered copy separately from the row identity.
/// SnapshotRevision identifies the installed Master snapshot and starts at zero.
/// Account name order and the separator determine generated account names.
/// Conflict priority defaults to Local; synchronization trigger defaults to ManualOnly.
/// Local settings and identity survive database replacement and are never merged with Master.
/// Services validate and persist local settings; this model carries configuration data.
/// </summary>
public class LocalConfig : BaseEntity
{
	public string LocalDatasetKey { get; set; } = string.Empty;
	public AccountNameOrder AccountNameOrder { get; set; } = AccountNameOrder.CorrespondentCategoryProject;
	public string AccountNameSeparator { get; set; } = "/";
	public bool AccountNameAddCurrency { get; set; }
	public ConflictPriority ConflictPriority { get; set; } = ConflictPriority.Local;
	public long SnapshotRevision { get; set; }
	public SyncTrigger SyncTrigger { get; set; } = SyncTrigger.ManualOnly;
}
